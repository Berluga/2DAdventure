using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

public class Character : MonoBehaviour, ISaveable
{
    [Header("事件监听")]
    public VoidEventSO newGameEvent;

    [Header("基本属性")]
    public float maxHealth;
    public float currentHealth;
    public float maxPower;
    public float currentPower;
    public float powerRecoverSpeed;
    public float powerRunningCost;

    [Header("受伤无敌")]
    public float invulnerableDuration;
    [HideInInspector]public float invulnerableCounter;
    public bool invulnerable;

    [Header("格挡")]
    public bool Defence;

    [Header("跑步")]
    public bool Running;

    public UnityEvent<Character> OnHealthChange;
    public UnityEvent<Transform> OnTakeDamage;
    public UnityEvent OnDie;
    private void OnEnable()
    {
        newGameEvent.OnEventRaised += NewGame;
        ISaveable saveable = this;
        saveable.RegisterSaveData();
    }

    private void OnDisable()
    {
        newGameEvent.OnEventRaised -= NewGame;
        ISaveable saveable = this;
        saveable.UnRegisterSaveData();

    }
    private void NewGame()
    {
        currentHealth = maxHealth;
        currentPower = maxPower;
        OnHealthChange?.Invoke(this);
    }

    private void Update()
    {
        if (invulnerable)
        {
            invulnerableCounter -= Time.deltaTime;
            if (invulnerableCounter <= 0)
            {
                invulnerable = false;
            }
        }


        if (currentPower < maxPower && !Running)
        {
            if(!Defence)
                currentPower += Time.deltaTime * powerRecoverSpeed;
            else
                currentPower += Time.deltaTime * powerRecoverSpeed* 0.2f ;//举盾时能量回复速度减慢
        }

        HandleEnergyConsumption();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Water"))
        {
            if (currentHealth > 0)
            {//死亡、更新血量
                currentHealth = 0;
                OnHealthChange?.Invoke(this);
                OnDie?.Invoke();
            }
        }
    }

    public void TakeDamage(Attack attacker)
    {
        if (invulnerable)
            return;
        if(Defence && currentPower >= 0)
        {
            // 举盾状态下消耗能量挡伤
            BlockDamage(attacker.damage/2);
            TriggerInvulnerable();
        }
        else
        {
            // 未举盾或能量不足，承受伤害
            if (currentHealth - attacker.damage > 0)
            {
                currentHealth -= attacker.damage;
                TriggerInvulnerable();
                //执行受伤
                OnTakeDamage?.Invoke(attacker.transform);
            }
            else
            {
                currentHealth = 0;
                //触发死亡
                OnDie?.Invoke();
            }

            OnHealthChange?.Invoke(this);
        }
    }

    private void BlockDamage(int defencePowerCost)
    {
        // 消耗能量（确保不小于0）
        currentPower = currentPower - defencePowerCost;
        Debug.Log($"挡住攻击!");
        OnHealthChange?.Invoke(this);

        // 能量为0时自动取消举盾（可选逻辑）
        if (currentPower <= 0)
        {
            Defence = false;
            Debug.Log("能量耗尽，举盾结束");
        }

        // TODO: 触发挡伤特效/音效（如盾牌碰撞特效）
    }

    public void TriggerInvulnerable()
    {
        if (!invulnerable)
        {
            invulnerable = true;
            invulnerableCounter = invulnerableDuration;
        }
    }

    private void HandleEnergyConsumption()
    {
        if (Running && currentPower > 0)
        {
            currentPower -= powerRunningCost * Time.deltaTime;
            currentPower = Mathf.Max(0, currentPower); // 能量不小于0
            OnHealthChange?.Invoke(this);

            if (currentPower <= 0)
            {
                Running = false; // 能量耗尽，停止跑步
                Debug.Log("能量耗尽，无法继续跑步");
            }
        }
    }

    public void OnSlide(int cost)
    {
        currentPower -= cost;
        OnHealthChange?.Invoke(this);
    }

    public DataDefination GetDataID()
    {
        return GetComponent<DataDefination>();
    }

    public void GetSaveData(Data data)
    {
        if (data.characterPosDict.ContainsKey(GetDataID().ID))
        {
            data.characterPosDict[GetDataID().ID] = new SerializeVector3(transform.position);
            data.floatSaveData[GetDataID().ID + "health"] = this.currentHealth;
            data.floatSaveData[GetDataID().ID + "power"] = this.currentPower;
        }
        else
        {
            data.characterPosDict.Add(GetDataID().ID, new SerializeVector3(transform.position));
            data.floatSaveData.Add(GetDataID().ID + "health", this.currentHealth);
            data.floatSaveData.Add(GetDataID().ID + "power", this.currentPower);
        }
    }

    public void LoadData(Data data)
    {
        if (data.characterPosDict.ContainsKey(GetDataID().ID))
        {
            this.currentHealth = data.floatSaveData[GetDataID().ID + "health"];
            this.currentPower = data.floatSaveData[GetDataID().ID + "power"];
            transform.position = data.characterPosDict[GetDataID().ID].ToVector3();

            //通知UI更新
            OnHealthChange?.Invoke(this);
        }
    }
}
