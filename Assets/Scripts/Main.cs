using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour {

    public Button btnA;
    public Button btnB;
    
    IEnumerator ChangeScene(string sceneName)
    {
        AsyncOperation aysync = Application.LoadLevelAsync(sceneName);
        yield return aysync;
    }
    public void ClickBtnA()
    {
        StartCoroutine(ChangeScene("BaiduVoice"));
    }
    public void ClickBtnB()
    {
        StartCoroutine(ChangeScene("TextToSpeech"));
    }
}
