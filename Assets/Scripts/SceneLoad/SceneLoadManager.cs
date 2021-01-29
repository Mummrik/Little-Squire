using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine;
enum sceneAmmount {singleScene, multipleScenes}

public class SceneLoadManager : MonoBehaviour
{
    private static SceneLoadManager instance = null;

    public delegate void OnSceneChangeStart();
    public static OnSceneChangeStart OnChangeScene;

    public delegate void OnReturnToSceneStart();
    public static OnReturnToSceneStart OnReturnToScene;

    public delegate void OnReloadScene();
    public static OnReloadScene OnRestartScene;

    //temp Scenes
    string tempFirstSceneLoaded;
    string[] tempSceneNameList;
    private bool isLoadingMultiScene = false;

    private List<string> tempCurrentScenes = new List<string>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(instance);
        }
        else
            Destroy(gameObject);

    }

    void OnEnable()
    {
        SceneLoadTrigger.onEnterSceneBound += OnLoadSubScenes;
        SceneLoadTrigger.onExitSceneBound += OnUnloadSubScenes;
        SceneLoadTrigger.onEnterNewSceneBound += OnSceneTransition;
        SceneLoadTrigger.onMergeAwakeScene += OnMergeStartScene;
        PauseMenu.OnRestartScene += OnResetScene;
    }

    void OnDisable()
    {
        SceneLoadTrigger.onEnterSceneBound -= OnLoadSubScenes;
        SceneLoadTrigger.onExitSceneBound -= OnUnloadSubScenes;
        SceneLoadTrigger.onEnterNewSceneBound -= OnSceneTransition;
        SceneLoadTrigger.onMergeAwakeScene -= OnMergeStartScene;
        PauseMenu.OnRestartScene -= OnResetScene;
    }

    void OnMergeStartScene(string sceneString)
    {
        StartCoroutine(MergeTheAwakeScene(sceneString));
    }

    public void OnResetScene()
    {
        OnRestartScene?.Invoke();
        List<string> tempCurrentScenesName = new List<string>();
        //GET CURRENT SCENES
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            tempCurrentScenesName.Add(SceneManager.GetSceneAt(i).name.ToString());
        }

        SceneManager.CreateScene("temp");

        for (int i = 0; i < SceneManager.sceneCount - 1; i++)
        {
            SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(i));
        }
        
        LoadSceneSingle(tempCurrentScenesName[0]);
        if (tempCurrentScenesName.Count > 1)
        {
            for (int i = 1; i < tempCurrentScenesName.Count; i++)
            {
                LoadSceneAdditive(tempCurrentScenesName[i]);
            }
        }
        //StartCoroutine(CheckIfLastSceneLoaded());
        if (tempCurrentScenesName[0].ToString() != SceneManager.GetSceneByBuildIndex(0).name)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
       }

        tempCurrentScenesName.Clear();
        OnChangeScene?.Invoke();
        Time.timeScale = 1.0f;
    }


    public void LoadSceneSingle(string SceneName)
    {
        if (SceneName != null)
        {
            if (!SceneManager.GetSceneByName(SceneName).isLoaded)
            {
                SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
                //Debug.Log(SceneName + " Loaded");
            }
        }
    }

    public void LoadSceneAdditive(string SceneName)
    {
        if (!SceneManager.GetSceneByName(SceneName).isLoaded)
            {
                SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
                //Debug.Log(SceneName + " Loaded");
            }
    }
    public void UnloadScene(string SceneName)
    {
        if (SceneManager.GetSceneByName(SceneName).isLoaded)
        {
            SceneManager.UnloadSceneAsync(SceneName);
            //Debug.Log(SceneName + " Unloaded");
        }
    }


    public void OnLoadSubScenes(string theScene, string[] multiSceneLoadList, bool isMultiModeLoad)
    {
        if (!isMultiModeLoad)
        {
            LoadSceneAdditive(theScene);
        }
        else if (isMultiModeLoad)
        {
            for (int i = 0; i < multiSceneLoadList.Length; i++)
            {
                LoadSceneAdditive(multiSceneLoadList[i].ToString());
            }
        }

    }

    public void OnUnloadSubScenes(string theScene, string[] multiSceneLoadList, bool isMultiModeLoad)
    {
        if (!isMultiModeLoad)
        {
            UnloadScene(theScene);
        }
        else if (isMultiModeLoad)
        {
            for (int p = 0; p < multiSceneLoadList.Length; p++)
            {
                UnloadScene(multiSceneLoadList[p].ToString());
            }
        }
    }

    public void OnSceneTransition(string theScene, string[] multiSceneLoadList, bool isMultiModeLoad)
    {
        OnReturnToScene?.Invoke();
        StartCoroutine(WhileSceneLoad(theScene, multiSceneLoadList, isMultiModeLoad));
    }

    IEnumerator WhileSceneLoad(string theSceneName, string[] multiSceneList, bool isMultiModeLoad)
    {
        yield return null;
        if (!isMultiModeLoad)
        {
            if (theSceneName != null)
            {
                tempFirstSceneLoaded = theSceneName;
            }
        }
        else if (isMultiModeLoad)
        {
            if (multiSceneList != null)
            {
                tempFirstSceneLoaded = multiSceneList[0];
                tempSceneNameList = multiSceneList;
            }
        }
        AsyncOperation asyncOperator;
        if (!SceneManager.GetSceneByName(tempFirstSceneLoaded).isLoaded)
        {
            asyncOperator = SceneManager.LoadSceneAsync(tempFirstSceneLoaded);
            asyncOperator.allowSceneActivation = false;
           // Debug.Log(asyncOperator.progress);
            yield return new WaitForSecondsRealtime(2);
           // Debug.Log(asyncOperator.progress);

            if (asyncOperator.progress >= 0.9f)
            {
                asyncOperator.allowSceneActivation = true;
                if (isMultiModeLoad)
                {
                    string tempSceneString;
                    for (int i = 1; i < multiSceneList.Length; i++)
                    {
                        tempSceneString = tempSceneNameList[i].ToString();
                        LoadSceneAdditive(tempSceneString);
                        yield return new WaitForSecondsRealtime(1);
                      // SceneManager.MergeScenes(SceneManager.GetSceneByName(tempSceneNameList[i].ToString()), SceneManager.GetSceneByName(tempFirstSceneLoaded));
                    }
                    OnChangeScene?.Invoke();
                }
                else
                {
                    OnChangeScene?.Invoke();
                }
            }
        }
        //else
            //Debug.LogWarning(theSceneName + " is already loaded");
    }

   IEnumerator CheckIfLastSceneLoaded()
    {
        yield return new WaitForEndOfFrame();
        while(!SceneManager.GetSceneAt(SceneManager.sceneCount).isLoaded)
        {
           // Debug.Log("loading...");
        }
        OnChangeScene?.Invoke();
    }

    IEnumerator MergeTheAwakeScene(string sceneString)
    {
        AsyncOperation asyncOperationAwake;
        asyncOperationAwake = SceneManager.LoadSceneAsync(sceneString, LoadSceneMode.Additive);
        while (!asyncOperationAwake.isDone)
        {
            yield return null;
        }
        yield return new WaitForEndOfFrame();
        asyncOperationAwake.allowSceneActivation = true;
        yield return new WaitForEndOfFrame();
        SceneManager.MergeScenes(SceneManager.GetSceneByName(sceneString), SceneManager.GetActiveScene());
        StopAllCoroutines();
    }

}
