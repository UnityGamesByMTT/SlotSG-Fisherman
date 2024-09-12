using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System;

public class SlotBehaviour : MonoBehaviour
{


    [Header("Sprites")]
    [SerializeField]
    private Sprite[] myImages;  //images taken initially

    [Header("Slot Images")]
    [SerializeField]
    private List<SlotImage> images;     //class to store total images
    [SerializeField]
    private List<SlotImage> slotmatrix;     //class to store the result matrix

    [Header("Slots Objects")]
    [SerializeField]
    private GameObject[] Slot_Objects;
    [Header("Slots Elements")]
    [SerializeField]
    private LayoutElement[] Slot_Elements;

    [Header("Slots Transforms")]
    [SerializeField]
    private Transform[] Slot_Transform;

    [Header("Line Button")]
    [SerializeField]
    private List<Button> StaticLine_Buttons;
    [Header("Line Button Objects")]
    [SerializeField]
    private List<ManageLineButtons> StaticLine_Scripts;

    private Dictionary<int, string> y_string = new Dictionary<int, string>();

    [Header("Buttons")]
    [SerializeField] private Button SlotStart_Button;
    [SerializeField] private Button AutoSpin_Button;
    [SerializeField] private Button AutoSpinStop_Button;
    

    [Header("Miscellaneous UI")]
    [SerializeField]
    private TMP_Text Balance_text;
    [SerializeField]
    private TMP_Text TotalBet_text;
    [SerializeField]
    private TMP_Text BetPerLine_text;
    [SerializeField]
    private TMP_Text Lines_text;
    [SerializeField]
    private TMP_Text TotalWin_text;
    [SerializeField]
    private Button MaxBet_Button;
    [SerializeField]
    private Button BetPlus_Button;
    [SerializeField]
    private Button BetMinus_Button;
    [SerializeField]
    private Button LinePlus_Button;
    [SerializeField]
    private Button LineMinus_Button;


    int tweenHeight = 0;  //calculate the height at which tweening is done

    [SerializeField]
    private GameObject Image_Prefab;    //icons prefab

    private Tweener WinTween=null;
    private List<Tweener> alltweens = new List<Tweener>();


    [SerializeField]
    private List<ImageAnimation> TempList;  //stores the sprites whose animation is running at present 

    [SerializeField]
    private int IconSizeFactor = 100;       //set this parameter according to the size of the icon and spacing

    private int numberOfSlots = 5;          //number of columns

    [SerializeField]
    int verticalVisibility = 3;

    [Header("scripts")]
    [SerializeField] private AudioController audioController;
    [SerializeField] private UIManager uIManager;
    [SerializeField] private SocketIOManager SocketManager;
    [SerializeField] private PayoutCalculation PayCalculator;
    [SerializeField] private Bonus_Controller bonus_Controller;

    Coroutine AutoSpinRoutine = null;
    Coroutine tweenroutine=null;
    bool IsAutoSpin = false;
    private bool IsSpinning = false;
    private int BetCounter = 0;
    private int LineCounter = 0;
    internal int linecounter = 12;
    private double currentTotalBet;
    private double currentBalance;
    internal bool CheckPopups = false;


    private void Start()
    {

        if (SlotStart_Button) SlotStart_Button.onClick.RemoveAllListeners();
        if (SlotStart_Button) SlotStart_Button.onClick.AddListener(delegate { StartSlots(); });

        if (BetPlus_Button) BetPlus_Button.onClick.RemoveAllListeners();
        if (BetPlus_Button) BetPlus_Button.onClick.AddListener(delegate { ChangeBet(true); });
        if (BetMinus_Button) BetMinus_Button.onClick.RemoveAllListeners();
        if (BetMinus_Button) BetMinus_Button.onClick.AddListener(delegate { ChangeBet(false); });

        if (AutoSpin_Button) AutoSpin_Button.onClick.RemoveAllListeners();
        if (AutoSpin_Button) AutoSpin_Button.onClick.AddListener(AutoSpin);


        if (AutoSpinStop_Button) AutoSpinStop_Button.onClick.RemoveAllListeners();
        if (AutoSpinStop_Button) AutoSpinStop_Button.onClick.AddListener(StopAutoSpin);


        if (Lines_text != null)
        {
            Lines_text.text = "12";
        }


        if (LinePlus_Button) LinePlus_Button.onClick.RemoveAllListeners();
        if (LinePlus_Button) LinePlus_Button.onClick.AddListener(delegate { ChangeLine(true); });
        if (LineMinus_Button) LineMinus_Button.onClick.RemoveAllListeners();
        if (LineMinus_Button) LineMinus_Button.onClick.AddListener(delegate { ChangeLine(false); });

        if (MaxBet_Button) MaxBet_Button.onClick.RemoveAllListeners();
        if (MaxBet_Button) MaxBet_Button.onClick.AddListener(MaxBet);


        tweenHeight = (13 * IconSizeFactor) - 280;

    }

        private void AutoSpin()
    {
        if (!IsAutoSpin)
        {

            IsAutoSpin = true;
            if (AutoSpinStop_Button) AutoSpinStop_Button.gameObject.SetActive(true);
            // if (AutoSpin_Button) AutoSpin_Button.gameObject.SetActive(false);
            ToggleButtonGrp(false);
            if (AutoSpinRoutine != null)
            {
                StopCoroutine(AutoSpinRoutine);
                AutoSpinRoutine = null;
            }
            AutoSpinRoutine = StartCoroutine(AutoSpinCoroutine());

        }
    }

    private void StopAutoSpin()
    {
        if (IsAutoSpin)
        {
            IsAutoSpin = false;
            if (AutoSpinStop_Button) AutoSpinStop_Button.gameObject.SetActive(false);
            if (AutoSpin_Button) AutoSpin_Button.gameObject.SetActive(true);
            StartCoroutine(StopAutoSpinCoroutine());
        }

    }

    private IEnumerator AutoSpinCoroutine()
    {
        while (IsAutoSpin)
        {
            StartSlots(IsAutoSpin);
            yield return tweenroutine;
        }
    }

    private IEnumerator StopAutoSpinCoroutine()
    {
        yield return new WaitUntil(() => !IsSpinning);
        if (AutoSpinRoutine != null || tweenroutine != null)
        {
            StopCoroutine(AutoSpinRoutine);
            StopCoroutine(tweenroutine);
            tweenroutine = null;
            AutoSpinRoutine = null;
            yield return new WaitForSeconds(0.1f);
            // StopCoroutine(StopAutoSpinCoroutine());
        }
        ToggleButtonGrp(true);
    }
    internal void FetchLines(string LineVal, int count)
    {
        y_string.Add(count + 1, LineVal);
        // StaticLine_Objects[count].SetActive(true);
    }

    //Generate Static Lines from button hovers
    internal void GenerateStaticLine(TMP_Text LineID_Text)
    {

        int LineID = 0;
        try
        {
            LineID = int.Parse(LineID_Text.text);
        }
        catch (Exception e)
        {
            Debug.Log("Exception while parsing " + e.Message);
        }
        List<int> y_points = null;
        y_points = y_string[LineID]?.Split(',')?.Select(Int32.Parse)?.ToList();
        print("line id "+LineID);
        PayCalculator.GeneratePayoutLinesBackend(y_points, y_points.Count, true);

    }

    //Destroy Static Lines from button hovers
    internal void DestroyStaticLine()
    {

        PayCalculator.ResetLines();

    }

    private void MaxBet()
    {
        if (audioController) audioController.PlayButtonAudio();
        BetCounter = SocketManager.initialData.Bets.Count - 1;
        if (TotalBet_text) TotalBet_text.text = (SocketManager.initialData.Bets[BetCounter] *SocketManager.initialData.Lines.Count).ToString();
        if(BetPerLine_text) BetPerLine_text.text=SocketManager.initialData.Bets[BetCounter].ToString();
    }



    void OnBetOne(bool IncDec)
    {
        // if (audioController) audioController.PlayButtonAudio();

        if (BetCounter < SocketManager.initialData.Bets.Count - 1)
        {
            BetCounter++;
        }
        else
        {
            BetCounter = 0;
        }
        Debug.Log("Index:" + BetCounter);
        currentTotalBet = SocketManager.initialData.Bets[BetCounter] * SocketManager.initialData.Lines.Count;
        if (TotalBet_text) TotalBet_text.text = currentTotalBet.ToString();
        if (BetPerLine_text) BetPerLine_text.text = SocketManager.initialData.Bets[BetCounter].ToString();
    }

    private void ChangeBet(bool IncDec)
    {
        if (audioController) audioController.PlayButtonAudio();
        if (IncDec)
        {
            if (BetCounter < SocketManager.initialData.Bets.Count - 1)
            {
                BetCounter++;
            }
        }
        else
        {
            if (BetCounter > 0)
            {
                BetCounter--;
            }
        }
        currentTotalBet = SocketManager.initialData.Bets[BetCounter] * SocketManager.initialData.Lines.Count;
        if (TotalBet_text) TotalBet_text.text = currentTotalBet.ToString();
        if (BetPerLine_text) BetPerLine_text.text = SocketManager.initialData.Bets[BetCounter].ToString();
    }
    internal void ChangeLine(bool IncDec)
    {

        if (audioController)
            audioController.PlayButtonAudio();



        PayCalculator.ResetLines();
        if (IncDec)
        {
            linecounter++;
        }
        else
        {
            linecounter--;
        }

        if (linecounter < 1)
        {
            linecounter = 1;

        }
        if (linecounter > 12)
        {
            linecounter = 12;
        }


        foreach (Button sb in StaticLine_Buttons)
        {
            sb.interactable = false;
        }

        foreach (ManageLineButtons sb in StaticLine_Scripts)
        {
            sb.isActive = false;
        }

        for (int i = 1; i <= linecounter; i++)
        {
            Debug.Log("run this code" + linecounter);
            Lines_text.text = i.ToString();
            StaticLine_Buttons[i - 1].interactable = true;
            StaticLine_Buttons[i + 11].interactable = true;
            StaticLine_Scripts[i - 1].isActive = true;
            StaticLine_Scripts[i + 11].isActive = true;
            GenerateStaticLine(Lines_text);
        }
    }

    //just for testing purposes delete on production
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && SlotStart_Button.interactable)
        {
            StartSlots();
        }
    }


    //COMPLETE: slot set ui properly at initial and multiparshhet
    internal void SetInitialUI()
    {
        BetCounter = 0;
        LineCounter = SocketManager.initialData.LinesCount.Count - 1;
        currentBalance = SocketManager.playerdata.Balance;
        currentTotalBet = SocketManager.initialData.Bets[BetCounter] * SocketManager.initialData.Lines.Count;
        if (TotalBet_text) TotalBet_text.text = currentTotalBet.ToString();
        Debug.Log("my bets is " + SocketManager.initialData.Bets[BetCounter]);
        if (Lines_text) Lines_text.text = SocketManager.initialData.LinesCount[LineCounter].ToString();
        if (TotalWin_text) TotalWin_text.text = SocketManager.playerdata.currentWining.ToString("f2");
        if (Balance_text) Balance_text.text = currentBalance.ToString("f2");
        if (BetPerLine_text) BetPerLine_text.text = SocketManager.initialData.Bets[BetCounter].ToString();
        uIManager.InitialiseUIData(SocketManager.initUIData.AbtLogo.link, SocketManager.initUIData.AbtLogo.logoSprite, SocketManager.initUIData.ToULink, SocketManager.initUIData.PopLink, SocketManager.initUIData.paylines);

    }

    internal void LayoutReset(int number)
    {
        // if (Slot_Elements[number]) Slot_Elements[number].ignoreLayout = true;
        // if (SlotStart_Button) SlotStart_Button.interactable = true;
    }

    internal void shuffleInitialMatrix()
    {

        for (int k = 0; k < slotmatrix.Count * 3; k++)
        {
            slotmatrix[k / 3].slotImages[k % 3].sprite = myImages[UnityEngine.Random.Range(0, myImages.Length)];
        }


        // GenerateMatrix(number);
    }

    //starts the spin process
    private void StartSlots(bool autoSpin=false)
    {

        if (audioController) audioController.PlayButtonAudio("spin");

        if (!autoSpin)
        {
            if (AutoSpinRoutine != null)
            {
                StopCoroutine(AutoSpinRoutine);
                StopCoroutine(tweenroutine);
                tweenroutine = null;
                AutoSpinRoutine = null;
            }
        }
        WinningsAnim(false);
        if (SlotStart_Button) SlotStart_Button.interactable = false;
        if (SlotStart_Button) SlotStart_Button.interactable = false;
        if (TempList.Count > 0)
        {
            StopGameAnimation();
        }
        WinningsAnim(false);
        PayCalculator.ResetLines();
        tweenroutine=StartCoroutine(TweenRoutine());

    }


    //COMPLETED: slot compare balance
    private IEnumerator TweenRoutine()
    {


        if (currentBalance < currentTotalBet)
        {
            CompareBalance();
            if (IsAutoSpin)
            {
                StopAutoSpin();
                yield return new WaitForSeconds(1f);

            }

            yield break;
        }
        IsSpinning = true;
        ToggleButtonGrp(false);
        if(audioController)audioController.PlaySpinBonusAudio();

        for (int i = 0; i < numberOfSlots; i++)
        {
            InitializeTweening(Slot_Transform[i]);
            yield return new WaitForSeconds(0.1f);
        }


        double bet = 0;
        double balance = 0;
        try
        {
            bet = double.Parse(TotalBet_text.text);
        }
        catch (Exception e)
        {
            Debug.Log("Error while conversion " + e.Message);
        }

        try
        {
            balance = double.Parse(Balance_text.text);
        }
        catch (Exception e)
        {
            Debug.Log("Error while conversion " + e.Message);
        }
        double initAmount = balance;

        balance = balance - bet;

        DOTween.To(() => initAmount, (val) => initAmount = val, balance, 0.8f).OnUpdate(() =>
        {
            if (Balance_text) Balance_text.text = initAmount.ToString("f2");
        });

        SocketManager.AccumulateResult(BetCounter);

        yield return new WaitUntil(() => SocketManager.isResultdone);



        //COMPLETED: slot populate result data

        for (int j = 0; j < SocketManager.resultData.ResultReel.Count; j++)
        {
            List<int> resultnum = SocketManager.resultData.FinalResultReel[j]?.Split(',')?.Select(Int32.Parse)?.ToList();
            for (int i = 0; i < 5; i++)
            {
                if (slotmatrix[i].slotImages[j]) slotmatrix[i].slotImages[j].sprite = myImages[resultnum[i]];
            }
        }

        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < numberOfSlots; i++)
        {
            yield return StopTweening(Slot_Transform[i], i);
        }

        if (audioController) audioController.StopApinBonusAudio();
        if (audioController) audioController.StopWLAaudio();
        yield return new WaitForSeconds(0.5f);

        //COMPLETED: slot chek result and payline and animation

        CheckPayoutLineBackend(SocketManager.resultData.linesToEmit, SocketManager.resultData.FinalsymbolsToEmit);
        KillAllTweens();

        //COMPLETED: slot check all wins

        CheckPopups = true;

        currentBalance = SocketManager.playerdata.Balance;
        if (SocketManager.resultData.jackpot > 0)
        {
            uIManager.PopulateWin(4, SocketManager.resultData.jackpot);
            yield return new WaitUntil(() => !CheckPopups);
            CheckPopups = true;

        }

        if (SocketManager.resultData.isBonus)
        {
            bonus_Controller.StartBonusGame(SocketManager.resultData.BonusResult);
            yield return new WaitUntil(() => bonus_Controller.isfinished);
            bonus_Controller.FinishBonusGame(ref CheckPopups);

        }
        else if (SocketManager.resultData.WinAmout >= currentTotalBet * 10 && SocketManager.resultData.WinAmout < currentTotalBet * 15 && SocketManager.resultData.jackpot == 0)
        {
            uIManager.PopulateWin(1, SocketManager.resultData.WinAmout);
        }
        else if (SocketManager.resultData.WinAmout >= currentTotalBet * 15 && SocketManager.resultData.WinAmout < currentTotalBet * 20 && SocketManager.resultData.jackpot == 0)
        {
            uIManager.PopulateWin(2, SocketManager.resultData.WinAmout);
        }
        else if (SocketManager.resultData.WinAmout >= currentTotalBet * 20 && SocketManager.resultData.jackpot == 0)
        {
            uIManager.PopulateWin(3, SocketManager.resultData.WinAmout);
        }
        else
        {
            CheckPopups = false;
        }


        if (TotalWin_text) TotalWin_text.text = SocketManager.playerdata.currentWining.ToString("f2");
        if (Balance_text) Balance_text.text = SocketManager.playerdata.Balance.ToString("f2");

        if (SocketManager.resultData.WinAmout > 0)
            WinningsAnim(true);

        yield return new WaitUntil(() => !CheckPopups);


        if (!IsAutoSpin)
        {
            ToggleButtonGrp(true);
            IsSpinning = false;
        }
        else
        {
            yield return new WaitForSeconds(2f);
            IsSpinning = false;
        }


    }

    void WinningsAnim(bool toggle)
    {
        if (toggle)
        {
            WinTween = TotalWin_text.gameObject.GetComponent<RectTransform>().DOScale(new Vector2(1.5f, 1.5f), 1f).SetLoops(-1, LoopType.Yoyo).SetDelay(0);
        }
        else
        {
            WinTween.Kill();
            TotalWin_text.gameObject.GetComponent<RectTransform>().localScale = Vector3.one;
        }
    }

       private void CompareBalance()
    {
        if (currentBalance < currentTotalBet)
        {
            uIManager.LowBalPopup();
            if (AutoSpin_Button) AutoSpin_Button.interactable = false;
            if (SlotStart_Button) SlotStart_Button.interactable = false;
        }
        else
        {
            if (AutoSpin_Button) AutoSpin_Button.interactable = true;
            if (SlotStart_Button) SlotStart_Button.interactable = true;
        }
    }
    internal double GetCurrentbetperLine()
    {
        return SocketManager.initialData.Bets[BetCounter];
    }
    internal void CallCloseSocket()
    {
        SocketManager.CloseSocket();
    }
    private void CallOnExitFunction()
    {
        CallCloseSocket();
        Application.ExternalCall("window.parent.postMessage", "onExit", "*");
    }

    void ToggleButtonGrp(bool toggle)
    {

        if (SlotStart_Button) SlotStart_Button.interactable = toggle;
        if (MaxBet_Button) MaxBet_Button.interactable = toggle;
        if (LinePlus_Button) LinePlus_Button.interactable = toggle;
        if (LineMinus_Button) LineMinus_Button.interactable = toggle;
        if (BetMinus_Button) BetMinus_Button.interactable = toggle;
        if (BetPlus_Button) BetPlus_Button.interactable = toggle;
        if(AutoSpin_Button) AutoSpin_Button.interactable=toggle;

    }
    //start the icons animation
    private void StartGameAnimation(GameObject animObjects)
    {
        ImageAnimation temp = animObjects.transform.GetChild(0).GetComponent<ImageAnimation>();
        animObjects.transform.GetChild(0).gameObject.SetActive(true);
        temp.StartAnimation();
        TempList.Add(temp);
    }

    //stop the icons animation
    private void StopGameAnimation()
    {
        for (int i = 0; i < TempList.Count; i++)
        {
            TempList[i].StopAnimation();
        }
    }

    private void CheckPayoutLineBackend(List<int> LineId, List<string> points_AnimString)
    {
        List<int> y_points = null;
        List<int> points_anim = null;
        if (LineId.Count > 0 || points_AnimString.Count > 0)
        {
            if (audioController) audioController.PlayWLAudio("win");

            for (int i = 0; i < LineId.Count; i++)
            {
                y_points = y_string[LineId[i] + 1]?.Split(',')?.Select(Int32.Parse)?.ToList();
                PayCalculator.GeneratePayoutLinesBackend(y_points, y_points.Count);
            }

            for (int i = 0; i < points_AnimString.Count; i++)
            {

                points_anim = points_AnimString[i]?.Split(',')?.Select(Int32.Parse)?.ToList();

                for (int k = 0; k < points_anim.Count; k++)
                {
                    if (points_anim[k] >= 10)
                    {
                        StartGameAnimation(slotmatrix[(points_anim[k] / 10) % 10].slotImages[points_anim[k] % 10].gameObject);
                    }
                    else
                    {
                        StartGameAnimation(slotmatrix[0].slotImages[points_anim[k]].gameObject);
                    }
                }
            }
        }

    }

    private void GenerateMatrix(int value)
    {
        for (int j = 0; j < 3; j++)
        {
            slotmatrix[value].slotImages.Add(images[value].slotImages[images[value].slotImages.Count - 5 + j]);
        }
    }

    #region TweeningCode
    private void InitializeTweening(Transform slotTransform)
    {
        slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 0);
        Tweener tweener = slotTransform.DOLocalMoveY(-tweenHeight, 0.2f).SetLoops(-1, LoopType.Restart).SetDelay(0);
        tweener.Play();
        alltweens.Add(tweener);
    }

    private IEnumerator StopTweening(Transform slotTransform, int index)
    {
        alltweens[index].Pause();
        int tweenpos = (3 * IconSizeFactor) - IconSizeFactor;
        alltweens[index] = slotTransform.DOLocalMoveY(-tweenpos + 100, 0.5f).SetEase(Ease.OutElastic);
        yield return new WaitForSeconds(0.2f);

    }


    private void KillAllTweens()
    {
        for (int i = 0; i < numberOfSlots; i++)
        {
            alltweens[i].Kill();
        }
        alltweens.Clear();

    }
    #endregion

}

[Serializable]
public class SlotImage
{
    public List<Image> slotImages = new List<Image>(10);
}

