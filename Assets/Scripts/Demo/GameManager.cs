using System.Collections;
using System.Collections.Generic;
using Global.Timer;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private GlobalDispatchTimer gdt;
    
    private void Start()
    {
        gdt = GlobalDispatchTimer.Instance;
    }
}
