using System;
using UnityEngine;

public class Game : MonoBehaviour
{
    public static Action GameStart;
    
    public GameObject MenuGO;
    public GameObject GamePlayUIGO;
    public GameObject TankGO;

    private void Awake()
    {
        InitializeGame();
    }

    private void Start()
    {
        StartOptions.SubscribeGameStart(ShowGamePlay);
    }

    private void InitializeGame() => ShowMenu();

    public void ShowMenu()
    {
        Activate(MenuGO, true);
        Activate(TankGO, false);
        Activate(GamePlayUIGO, false);
    }
    
    private void ShowGamePlay()
    {
        Activate(MenuGO, false);
        Activate(TankGO, true);
        Activate(GamePlayUIGO, true);
    }

    private void Activate(GameObject go, bool activate) => 
        go.SetActive(activate);
}
