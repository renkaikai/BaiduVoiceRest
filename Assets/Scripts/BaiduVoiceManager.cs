using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;
using System.Text;
using System;

public class BaiduVoiceManager : MonoBehaviour
{
    private string token;       //access_token
    private string cuid = "00";     //用户唯一标识，用来区分用户，一般为机器MAC地址或IMEI码  一定要填 用于识别机器
    private string format = "wav";                  //语音格式
    private int rate = 8000;                        //采样率
    private int channel = 1;                        //声道数
    private string speech;                          //语音数据，进行base64编码
    private int len;                                //原始语音长度
    private string lan = "zh";                      //语种

    private string grant_Type = "client_credentials";
    //这两个需要到yuyin.baidu.com/app创建应用，查看key
    private string client_ID = "60StbijENqy6mQy4LfKaZ0qT";                               //百度appkey O9hqE7qG1GClCLCyBCFfdeG7
    private string client_Secret = "ZoSz8q8QhEnqGgixM3QGhM3gbWzjyydH";                   //百度Secret Key a571f509c3821171a3041b1ecd99badb

    //private string baiduAPI = "http://vop.baidu.com/server_api";

    enum RequestTheAddress
    {
        ASR,//语音识别
        SS, //语音合成
    }

    private string getTokenAPIPath = "https://openapi.baidu.com/oauth/2.0/token";

    private byte[] clipByte;

    public Text btnText;  //按钮文字
    public Text debugText;

    /// <summary>
    /// 转换出来的TEXT
    /// </summary>
    public static string audioToString;

    private AudioSource aud;
    private int audioLength;//录音的长度

    //-------Text to Speech------//
    [Header("Text to Speech")]
    public InputField inputField;


    private int spd = 5;
    private int pit = 5;
    private int vol = 5;
    private int per = 0;
    private int aue = 3;

    /// <summary>
    /// 获取百度token，有token才有权使用api
    /// </summary>
    /// <param name="url">获取的url</param>
    /// <returns></returns>
    private IEnumerator GetToken(string url, RequestTheAddress request)
    {
        string Address = "";
        WWWForm getTForm = new WWWForm();
        getTForm.AddField("grant_type", grant_Type);
        getTForm.AddField("client_id", client_ID);
        getTForm.AddField("client_secret", client_Secret);

        WWW getTW = new WWW(url, getTForm);
        yield return getTW;
        if (getTW.isDone)
        {
            if (getTW.error == null)
            {
                token = JsonMapper.ToObject(getTW.text)["access_token"].ToString();
                switch (request)
                {
                    case RequestTheAddress.ASR:
                        Address = "http://vop.baidu.com/server_api";
                        StartCoroutine(GetAudioString(Address));
                        break;
                    case RequestTheAddress.SS:
                        Address = "http://tsn.baidu.com/text2audio";
                        StartCoroutine(GetStringAudio(Address));
                        break;
                    default:
                        throw new Exception(request.ToString());
                        break;
                }

            }
            else
                Debug.LogError(getTW.error);
        }
    }

    /// <summary>
    /// 把语音转换为文字
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private IEnumerator GetAudioString(string url)
    {
        JsonWriter jw = new JsonWriter();
        jw.WriteObjectStart();
        jw.WritePropertyName("format");
        jw.Write(format);
        jw.WritePropertyName("rate");
        jw.Write(rate);
        jw.WritePropertyName("channel");
        jw.Write(channel);
        jw.WritePropertyName("token");
        jw.Write(token);
        jw.WritePropertyName("cuid");
        jw.Write(cuid);
        jw.WritePropertyName("len");
        jw.Write(len);
        jw.WritePropertyName("speech");
        jw.Write(speech);
        jw.WriteObjectEnd();
        WWWForm w = new WWWForm();

        WWW getASW = new WWW(url, Encoding.Default.GetBytes(jw.ToString()));
        yield return getASW;
        if (getASW.isDone)
        {
            if (getASW.error == null)
            {
                JsonData getASWJson = JsonMapper.ToObject(getASW.text);
                if (getASWJson["err_msg"].ToString() == "success.")
                {
                    audioToString = getASWJson["result"][0].ToString();
                    if (audioToString.Substring(audioToString.Length - 1) == "，")
                        audioToString = audioToString.Substring(0, audioToString.Length - 1);
                    Debug.Log(audioToString);
                }
            }
            else
            {
                Debug.LogError(getASW.error);
            }
        }
    }


    private bool writeFile(byte[] readByte, string fileName)
    {
        FileStream pFileStream = null;
        try
        {
            pFileStream = new FileStream(fileName, FileMode.OpenOrCreate);
            pFileStream.Write(readByte, 0, readByte.Length);
        }
        catch
        {
            return false;
        }
        finally
        {
            if (pFileStream != null)
            {
                pFileStream.Close();
            }
        }
        return true;
    }


    /// <summary>
    /// 文字转语音
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private IEnumerator GetStringAudio(string url)
    {
        WWWForm getTForm = new WWWForm();
        getTForm.AddField("lan", lan);
        getTForm.AddField("ctp", 1);
        getTForm.AddField("cuid", cuid);
        getTForm.AddField("tok", token);
        getTForm.AddField("tex", /*WWW.EscapeURL(*/inputField.text/*)*/);
        getTForm.AddField("vol", vol);
        getTForm.AddField("per", per);
        getTForm.AddField("spd", spd);
        getTForm.AddField("pit", pit);

        WWW getASW = new WWW(url, getTForm);
        yield return getASW;
        byte[] s = getASW.bytes;
        string filePath = Application.persistentDataPath + "/1.mp3";
        File.Delete(filePath);
        if (writeFile(s, filePath))
        {
            print(s.Length);
        }
        else
        {
            print("fail");
        }
        string path = "";
#if UNITY_EDITOR
        path = "file://" + filePath;
#elif UNITY_ANDROID
        path = "file://" + filePath;
#endif
        WWW w = new WWW(path);
        print(w.url);
        aud.clip = w.GetAudioClip(false, false, AudioType.MPEG);

        if (getASW.isDone)
        {
            if (getASW.error == null)
            {
                print("合成成功" + getASW.bytesDownloaded);
            }
            else
            {
                print(getASW.error);
            }
        }
    }

    /// <summary>
    /// 开始录音
    /// </summary>
    public void StartMic()
    {
        print(Microphone.devices.Length);
        if (Microphone.devices.Length == 0) return;
        Microphone.End(null);
        Debug.Log("Start");
        aud.clip = Microphone.Start(null, false, 10, rate);
    }

    /// <summary>
    /// 结束录音
    /// </summary>
    public void EndMic()
    {
        int lastPos = Microphone.GetPosition(null);
        if (Microphone.IsRecording(null))
            audioLength = lastPos / rate;//录音时长  
        else
            audioLength = 10;
        Debug.Log("Stop");
        Microphone.End(null);

        clipByte = GetClipData();
        len = clipByte.Length;
        speech = Convert.ToBase64String(clipByte);
        StartCoroutine(GetToken(getTokenAPIPath, RequestTheAddress.ASR));
        Debug.Log(len);
        Debug.Log(audioLength);
    }

    /// <summary>
    /// 把录音转换为Byte[]
    /// </summary>
    /// <returns></returns>
    public byte[] GetClipData()
    {
        if (aud.clip == null)
        {
            Debug.LogError("录音数据为空");
            return null;
        }

        float[] samples = new float[aud.clip.samples];

        aud.clip.GetData(samples, 0);


        byte[] outData = new byte[samples.Length * 2];

        int rescaleFactor = 32767; //to convert float to Int16   

        for (int i = 0; i < samples.Length; i++)
        {
            short temshort = (short)(samples[i] * rescaleFactor);

            byte[] temdata = System.BitConverter.GetBytes(temshort);

            outData[i * 2] = temdata[0];
            outData[i * 2 + 1] = temdata[1];
        }
        if (outData == null || outData.Length <= 0)
        {
            Debug.LogError("录音数据为空");
            return null;
        }
        //return SubByte(outData, 0, audioLength * 8000 * 2);
        return outData;
    }

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    void Awake()
    {
        if (GetComponent<AudioSource>() == null)
        {
            aud = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            aud = gameObject.GetComponent<AudioSource>();
        }
        aud.playOnAwake = true;
    }

    void Update()
    {
        if (audioToString == null || aud.clip == null) return;
        if (audioToString != null)
        //Debug.Log(audioToString);
        {
            debugText.text = "你刚刚说了如下内容:\n" + audioToString;
        };
        if (aud.clip != null)
        {
            if (!aud.isPlaying)
            {
                aud.Play();
            }
        }

    }


    private bool IsDown = false;


    public void OnBtnDown()
    {
        IsDown = true;
        btnText.text = "正在说话...";
        StartMic();
    }

    public void OnBtnUp()
    {
        IsDown = false;
        btnText.text = "开始说话";
        EndMic();
    }

    public void SpeechBtn()
    {
        StartCoroutine(GetToken(getTokenAPIPath, RequestTheAddress.SS));
    }

    public void BackMain()
    {
        StartCoroutine(MainScene());
    }
    IEnumerator MainScene()
    {
        AsyncOperation aysync = Application.LoadLevelAsync("Main");
        yield return aysync;
    }
}
