using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SavePoint : MonoBehaviour, IInteractable,ISaveable
{
    [Header("广播")]
    public VoidEventSO saveDataEvent;

    [Header("变量参数")]
    public SpriteRenderer spriteRenderer;
    public GameObject lightObj;
    public Sprite darkSprite;
    public Sprite lightSprite;
    public bool isDone;

    private void OnEnable()
    {
        ISaveable saveable = this;
        saveable.RegisterSaveData();

        // 注册后立即加载已保存的数据
        if (DataManager.instance != null)
        {
            saveable.LoadData(DataManager.instance.saveData);
        }
    }

    private void OnDisable()
    {
        ISaveable saveable = this;
        saveable.UnRegisterSaveData();
    }

    public void TriggerAction()
    {
        if (!isDone)
        {
            isDone = true;
            spriteRenderer.sprite = lightSprite;
            lightObj.SetActive(true);
            //TODO:保存数据
            saveDataEvent.RaiseEvent();
            Debug.Log("Save!");

            this.gameObject.tag = "Untagged";
        }
    }

    public DataDefination GetDataID()
    {
        return GetComponent<DataDefination>();
    }

    public void GetSaveData(Data data)
    {
        if (data.boolSaveData.ContainsKey(GetDataID().ID + "isDone"))
        {
            data.boolSaveData[GetDataID().ID + "isDone"] = this.isDone;
        }
        else
        {
            data.boolSaveData.Add(GetDataID().ID + "isDone", this.isDone);
        }
    }

    public void LoadData(Data data)
    {
        if (data.boolSaveData.ContainsKey(GetDataID().ID + "isDone"))
        {
            this.isDone = data.boolSaveData[GetDataID().ID + "isDone"];
            // 更新UI状态
            spriteRenderer.sprite = isDone ? lightSprite : darkSprite;
            lightObj.SetActive(isDone);
            // 如果已经激活，更新标签
            if (isDone)
            {
                this.gameObject.tag = "Untagged";
            }
        }

    }
}
