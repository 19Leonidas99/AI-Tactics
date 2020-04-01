﻿using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class menuCTRL : MonoBehaviour
{
    public Image mainBG;
    public GameObject[] bot;
    public RectTransform[] menu;
    public TextMeshProUGUI[] name, percentage, txtShowoff;
    public TextMeshProUGUI toolTipTxt, vText, roundsTxt;
    public TMP_Dropdown[] dd_Haxbot, dd_Intelli, dd_aiType;
    public Image bCol;
    public RectTransform title;
    public Scrollbar[] scroll;
    public Transform[] panelSwitch;
    public Button createAI;
    public TMP_InputField brainInp;
    public Slider[] percSlider, sliShowoff;
    public Scrollbar[] scro_Learning;
    public RectTransform msgBox;
    public Slider roundsSli;
    public GameObject flames;

    int multiplier = 1;

    HaxbotData[] hbD;
    List<TMP_Dropdown.OptionData> dd = new List<TMP_Dropdown.OptionData>();
    bool botsExist, brainsExist;
    string botsPath, brainsPath;
    bool[] nn_strat => new bool[] { Mathf.RoundToInt(scroll[0].value) == 1, Mathf.RoundToInt(scroll[1].value) == 1, Mathf.RoundToInt(scroll[2].value) == 1, Mathf.RoundToInt(scroll[3].value) == 1, };
    List<string> newAI_Config = new List<string>();
    int menuState;
    bool[] _isNN => new bool[] { GM.intelli[0] == null || GM.intelli[0].isHuman ? false : GM.intelli[0].usingAttackNN, GM.intelli[1] == null || GM.intelli[1].isHuman ? false : GM.intelli[1].usingAttackNN };
    bool matchHasNN => _isNN[0] || _isNN[1];

    // Start is called before the first frame update
    void Start()
    {
        hbD = new HaxbotData[2];

        LoadBattleData();

        // 
        MenuInit();

        vText.text = $"v{Application.version}";
    }

    void LoadBattleData()
    {
        botsPath = Path.Combine(Application.dataPath, "Bots");
        brainsPath = Path.Combine(Application.dataPath, "Brains");

        botsExist = Directory.Exists(botsPath);
        brainsExist = Directory.Exists(brainsPath);

        // 
        if (botsExist)
        {
            // 
            string[] allBots = Directory.GetDirectories(botsPath);

            dd = new List<TMP_Dropdown.OptionData>();
            List<TMP_Dropdown.OptionData> dd2 = new List<TMP_Dropdown.OptionData>();

            // 
            dd2.Add(new TMP_Dropdown.OptionData("Human"));

            // 
            if (brainsExist)
            {
                // 
                string[] aI_Config = Directory.GetFiles(brainsPath);

                // reset
                newAI_Config = new List<string>();

                // 
                for (int i = 0; i < aI_Config.Length; i++)
                {
                    if (aI_Config[i].Contains(".sbr") && !aI_Config[i].Contains(".meta"))
                    {
                        newAI_Config.Add(aI_Config[i]);
                    }
                }

                // 
                for (int i = 0; i < newAI_Config.Count; i++)
                {
                    string name = "";

                    // 
                    {
                        // 
                        using (StreamReader sR = new StreamReader(newAI_Config[i]))
                        {
                            name = sR.ReadToEnd().Split('\n')[0];
                        }

                        // 
                        dd2.Add(new TMP_Dropdown.OptionData(name));
                    }
                }
            }

            // 
            for (int i = 0; i < allBots.Length; i++)
            {
                dd.Add(new TMP_Dropdown.OptionData(allBots[i].Split('\\')[2]));
            }

            // 
            for (int i = 0; i < 2; i++)
            {
                dd_Haxbot[i].ClearOptions();
                dd_Intelli[i].ClearOptions();

                // 
                dd_Haxbot[i].AddOptions(dd);
                //
                dd_Intelli[i].AddOptions(dd2);
                // 
                LoadNSetHaxbot(i);
            }
        }
    }

    void MenuInit()
    {
        // 
        roundsSli.value = GM.totalRounds;

        // 
        for (int i = 0; i < 2; i++)
        {
            sliShowoff[i].value = GM.explSetter[i];
            scro_Learning[i].value = GM.nnIsLearning[i] ? 0 : 1;
        }

        // 
        dd_Haxbot[0].onValueChanged.AddListener(delegate { LoadNSetHaxbot(0); });
        dd_Haxbot[1].onValueChanged.AddListener(delegate { LoadNSetHaxbot(1); });

        // 
        dd_Intelli[0].onValueChanged.AddListener(delegate { SetIntelli(0); });
        dd_Intelli[1].onValueChanged.AddListener(delegate { SetIntelli(1); });

        // 
        UpdatePanel(0);
        UpdatePanel(1);
        UpdatePanel(2);
        UpdatePanel(3);

        // set the proper menu
        ChangeMenu(0);
        CheckIfCanCreateAI();
    }

    public void ToggleLearningSlider(int i)
    {
        GM.nnIsLearning[i] = scro_Learning[i].value == 0 ? true : false;

        Color[] set = new Color[2];

        ColorUtility.TryParseHtmlString("#A1FF7E", out set[0]);
        ColorUtility.TryParseHtmlString("#FF6843", out set[1]);

        scro_Learning[i].GetComponent<Image>().color = GM.nnIsLearning[i] ? set[0] : set[1];
    }

    public void SliderValChanged()
    {
        GM.totalRounds = (int)roundsSli.value * multiplier;
        roundsTxt.text = $"Rounds {(multiplier > 1 ? $"x{multiplier}" : "")}: {GM.totalRounds}";
    }

    public void SliderShowoffChanged(int which)
    {
        GM.explSetter[which] = sliShowoff[which].value;
        txtShowoff[which].text = $"Showoff: {Mathf.Round(GM.explSetter[which] * 100)}%";
    }

    public void UpdatePercentage(int who)
    {
        percentage[who].text = $"{Mathf.Round(percSlider[who].value * 100)}%";
    }

    public void CreateAIBrain()
    {
        // just a random hyperparam that I just chose at whim, 4 nodes per 1 output node for the HL
        int hlNodesPer1 = 4;
        // inputs are 26 different values
        int inpCount = 26;
        // attack constant output nodes
        int attOutNodeCount = 3;
        // attack HL node count
        int attHLNodeCount = hlNodesPer1 * attOutNodeCount;

        // calulate how many weights we got for attack weight
        int attWeightCount = (inpCount * attHLNodeCount) + (attHLNodeCount * attOutNodeCount);

        // 
        string[] strat = new string[4];

        // 
        int[] weightCount = new int[] { 84, 276, 84, attWeightCount };

        // 
        string ret = "";

        // 
        for (int j = 0; j < weightCount.Length; j++)
        {
            string ch = "";
            bool last = j == weightCount.Length - 1;

            // 
            if (nn_strat[j])
            {
                ch = $"{dd_aiType[j].value - 1}";
            }
            else
            {

                ch = last ? $"{percSlider[j].value},{percSlider[j + 1].value},{percSlider[j + 2].value}" : $"{percSlider[j].value}";
                ret += $"{ch}{(last ? "" : ";")}";
                continue;
            }

            //
            for (int i = 0; i < weightCount[j]; i++)
            {
                strat[j] += $"{(ch == "-1" ? Random.Range(-1f, 1f).ToString() : ch)}{(i != weightCount[j] - 1 ? "," : "")}";
            }

            // 
            ret += $"{strat[j]}{(last ? "" : ";")}";
        }

        // 
        string bPath = Path.Combine(Application.dataPath, "Brains");

        // 
        if (!Directory.Exists(bPath))
        {
            Directory.CreateDirectory(bPath);
        }

        // 
        using (StreamWriter sW = new StreamWriter(Path.Combine(bPath, $"{brainInp.text}.sbr")))
        {
            sW.Write($"{brainInp.text}\n{ret}");
        }

        StartCoroutine(ShowMessage("Your AI Brain has been Saved in the data file/Brains!"));
    }

    public void CheckIfCanCreateAI()
    {
        createAI.interactable = brainInp.text != "";
    }

    public void UpdatePanel(int curr)
    {
        int which = curr * 2;

        scroll[curr].GetComponent<Image>().color = scroll[curr].value == 0 ? new Color32(114, 255, 90, 255) : new Color32(144, 90, 255, 255);
        scroll[curr].GetComponentInChildren<TextMeshProUGUI>().text = scroll[curr].value == 0 ? "Traditional" : "Neural Network";
        panelSwitch[which].gameObject.SetActive(scroll[curr].value == 0);
        panelSwitch[which + 1].gameObject.SetActive(scroll[curr].value != 0);
    }

    void LoadNSetHaxbot(int i)
    {
        hbD[i] = HXB.LoadHaxbot(bot[i], dd[dd_Haxbot[i].value].text);
        name[i].text = hbD[i].name;
        GM.hbName[i] = hbD[i].name;

        // 
        for (int j = 0; j < 2; j++)
        {
            foreach (Renderer r in bot[j].GetComponentsInChildren<Renderer>())
            {
                r.material.SetTexture(GM.mainTexture, hbD[j].txt2d);
            }
        }

    }

    void SetIntelli(int i)
    {
        int chosen = dd_Intelli[i].value;

        GM.intelli[i] = chosen == 0 ? new AI_Config("Human", "Human\nx") : AI.LoadIntelligence(newAI_Config[chosen - 1]);

        sliShowoff[i].gameObject.SetActive(_isNN[i]);
        scro_Learning[i].gameObject.SetActive(_isNN[i]);
    }

    // Update is called once per frame
    void Update()
    {
        bCol.color = Color.Lerp(Color.white, Color.red, Mathf.Abs(Mathf.Sin(Time.time)));
        title.localRotation = Quaternion.Euler(Vector3.forward * Mathf.Sin(Time.time * 5) * 2);

        // 
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        if (menuState == 1)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                multiplier++;

                multiplier = Mathf.Clamp(multiplier, 1, multiplier);
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                multiplier--;

                multiplier = Mathf.Clamp(multiplier, 1, multiplier);
            }

            SliderValChanged();
        }
    }

    IEnumerator ShowMessage(string msg)
    {
        msgBox.gameObject.SetActive(true);
        msgBox.GetComponentInChildren<TextMeshProUGUI>().text = msg;
        yield return new WaitForSeconds(3);
        msgBox.gameObject.SetActive(false);
    }

    public void ChangeMenu(int w)
    {
        menuState = w;

        if (w == 1)
        {
            // 
            SetIntelli(0);
            SetIntelli(1);
            LoadBattleData();
        }

        flames.gameObject.SetActive(w == 1); // ? new Vector3(0.19f, -1.81f, 1.47f) : Vector3.up * -10000;

        LoadBattleData();
        Color32[] col = new Color32[] { new Color32(213, 202, 255, 255), new Color32(63, 63, 63, 255), new Color32(81, 183, 255, 255) };

        Camera.main.backgroundColor = col[w];

        // 
        for (int i = 0; i < menu.Length; i++)
        {
            menu[i].gameObject.SetActive(i == w);
        }
    }

    public void GoTo(string where)
    {
        SceneManager.LoadScene(where);
    }

    public void SetToolTip(string tt)
    {
        toolTipTxt.gameObject.SetActive(tt != "");
        toolTipTxt.text = tt;
    }
}
