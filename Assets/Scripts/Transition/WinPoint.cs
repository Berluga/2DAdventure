using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WinPoint : MonoBehaviour, IInteractable
{
    public VoidEventSO winEventSO;
 

    public void TriggerAction()
    {

        winEventSO.RaiseEvent();

        this.gameObject.tag = "Untagged";
    }
}
