
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System;
using UnityEngine.UI;

public class clock2_manager : UdonSharpBehaviour
{
    #region SerializeField
    //-----------------------------------
    //22:09
    //22:08
    //22:10
    //22:07
    //のときに 07がまるまるスキップされてしまった。

    [SerializeField] GameObject shortHand;
    [SerializeField] GameObject longHand;
    [SerializeField] GameObject secondHand;


    [Header("この時計でこのワールドのSkyboxを制御する")]
    [SerializeField] bool controlSkybox = false;
    [Header("Cloud Sea")]
    [SerializeField] GameObject gmObjCloudsea;

    [Header("この時計でこのワールドのBGMを制御する")]
    [SerializeField] bool controlMusic = false;
    [SerializeField, Range(0, 25)] int fadeInTime = 0;
    [SerializeField, Range(0, 25)] int fadeOutTime = 15;
    [Header("AudioSource")]
    [SerializeField] AudioSource audioSrc;


    [SerializeField, Multiline] String chk_str = "";
    [SerializeField, Multiline] String consoleStr2 = "";
    [SerializeField, Multiline] String consoleStr3 = "";
    [SerializeField, Multiline] String consoleStr4 = "";
    [SerializeField, Multiline] String consoleStr5 = "";
    [SerializeField, Range(0f, 1f)] float chk_vol = 0f;
    [SerializeField, Range(0f, 30f)] float chk_rem = 0f;

    [Header("開始時刻 / 早朝")]
    [SerializeField, Range(0, 23)] int morningHour = 5;
    [SerializeField, Range(0, 59)] int morningMin = 00;
    [SerializeField] Material skyboxMorning;
    [SerializeField] Material cloudseaMorning;
    [SerializeField] AudioClip audioMorning;
    [SerializeField] GameObject directionalLightMorning;

    [Header("開始時刻 / 昼")]
    [SerializeField, Range(0, 23)] int dayHour = 7;
    [SerializeField, Range(0, 59)] int dayMin = 00;
    [SerializeField] Material skyboxDay;
    [SerializeField] Material cloudseaDay;
    [SerializeField] AudioClip audioDay;
    [SerializeField] GameObject directionalLightDay;

    [Header("開始時刻 / 夕方")]
    [SerializeField, Range(0, 23)] int eveningHour = 17;
    [SerializeField, Range(0, 59)] int eveningMin = 00;
    [SerializeField] Material skyboxEvening;
    [SerializeField] Material cloudseaEvening;
    [SerializeField] AudioClip audioEvening;
    [SerializeField] GameObject directionalLightEvening;

    [Header("開始時刻 / 夜")]
    [SerializeField, Range(0, 23)] int nightHour = 19;
    [SerializeField, Range(0, 59)] int nightMin = 00;
    [SerializeField] Material skyboxNight;
    [SerializeField] Material cloudseaNight;
    [SerializeField] AudioClip audioNight;
    [SerializeField] GameObject directionalLightNight;

    #endregion


    private float fadeInMax;
    private float fadeOutMax;
    private float currentVol;

    //計算用
    private float fadeInMax_INV;
    private float fadeOutMax_INV;

    private int[] startsHour;
    private int[] startsMin;

    private int prevDay = -1;
    DateTime[] switchingDTs;

    DateTime[] fadeoutStartDTs;

    Material[] switchingMats;
    Material[] switchingClouds;

    AudioClip[] switchingClips;
    GameObject[] switchingLights;

    //string[] stDateTime = new string[] { "Morning", "Day", "Evening", "Night" };
    //private int[] sortedDtPos = new int[] { 0, 0, 0, 0 };
    private int[] DtOfThisPos = new int[] { 0, 0, 0, 0 };
    private int[] DtPrevOfThisPos = new int[] { 0, 0, 0, 0 };

    private int prevDT = -1;
    private int prevDT_Audio = -1;

    private int currentAudioStat = AUDIO_IDLE;
    private const int AUDIO_IDLE = 0;
    private const int AUDIO_FADING_OUT = 51;
    private const int AUDIO_FADING_IN = 52;
    private const int AUDIO_PLAYING = 53;
    private float audioRemainFadeTime = 0;
    private bool thisisFirstFade = true;

    [Header("秒針拡張(触らないことをお勧めします)")]
    [SerializeField] bool overTickEnabled = true;

    private float overTickRemainTime = 0.0f;
    private float overTickTimeMax = 0.2f;
    private bool overTicking = false;
    private int prevSec = -1;


    void Start()
    {

        //空
        switchingMats = new Material[] { skyboxMorning, skyboxDay, skyboxEvening, skyboxNight };
        switchingClouds = new Material[] { cloudseaMorning, cloudseaDay, cloudseaEvening, cloudseaNight };
        switchingLights = new GameObject[] {
            directionalLightMorning, directionalLightDay, directionalLightEvening, directionalLightNight
        };
        foreach (GameObject gmObj in switchingLights)
        {
            if (gmObj != null) gmObj.SetActive(false);
        }

        //音
        switchingClips = new AudioClip[] { audioMorning, audioDay, audioEvening, audioNight };
        fadeInMax = fadeInTime + 0.1f;
        fadeOutMax = fadeOutTime + 0.1f;
        fadeInMax_INV = 1 / fadeInMax;
        fadeOutMax_INV = 1 / fadeOutMax;


        //-------------------------------------------

        DateTime dtNow = DateTime.Now;

        switchingDTs = new DateTime[]
        {
            new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, morningHour,morningMin,0),
            new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, dayHour,dayMin,0),
            new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, eveningHour,eveningMin,0),
            new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, nightHour,nightMin,0),
        };

        //フェードアウト発動タイミングを計算しておく
        //1secは発動予備マージン
        TimeSpan ts_fadeout = new TimeSpan(0, 0, fadeOutTime + 1);

        fadeoutStartDTs = new DateTime[]
        {
            switchingDTs[0] - ts_fadeout,
            switchingDTs[1] - ts_fadeout,
            switchingDTs[2] - ts_fadeout,
            switchingDTs[3] - ts_fadeout,
        };

        //現在日付基準で更新(最初も必要)
        RecalcSwitchingDT(dtNow);

        consoleStr2 = fadeoutStartDTs[0].ToString();
        consoleStr3 = fadeoutStartDTs[1].ToString();
        consoleStr4 = fadeoutStartDTs[2].ToString();
        consoleStr5 = fadeoutStartDTs[3].ToString();

        //時間帯ソート
        int[] sortedDtPos = new int[] { 0, 0, 0, 0 };
        int[] sortedDtPrevPos = new int[] { 0, 0, 0, 0 };
        DtOfThisPos = new int[] { 0, 0, 0, 0 };
        DtPrevOfThisPos = new int[] { 0, 0, 0, 0 };
        for (int i = 0; i < 4; i++)
        {
            for (int k = 0; k < 4; k++)
            {
                if (i == k) continue;
                if (switchingDTs[i] > switchingDTs[k]) sortedDtPos[i]++;
                if (fadeoutStartDTs[i] > fadeoutStartDTs[k]) sortedDtPrevPos[i]++;

            }
            DtOfThisPos[sortedDtPos[i]] = i;
            DtPrevOfThisPos[sortedDtPrevPos[i]] = i;
        }


    }

    private void RecalcSwitchingDT(DateTime dtNow)
    {

        for (int i = 0; i < 4; i++)
        {
            switchingDTs[i] = new DateTime(
                dtNow.Year,
                dtNow.Month,
                dtNow.Day,
                switchingDTs[i].Hour,
                switchingDTs[i].Minute,
                0);

            fadeoutStartDTs[i] = new DateTime(
                dtNow.Year,
                dtNow.Month,
                dtNow.Day,
                fadeoutStartDTs[i].Hour,
                fadeoutStartDTs[i].Minute,
                fadeoutStartDTs[i].Second);
        }

    }

    private void Clock_Tick(int min, int hour, int sec)
    {
        if (longHand != null) longHand.transform.localEulerAngles =
                  new Vector3(0, 0, -6.0f * min);

        if (shortHand != null) shortHand.transform.localEulerAngles =
                    new Vector3(0, 0, -30.0f * hour + -0.5f * min);


        float over = 0f;

        if (overTicking && overTickEnabled)
        {
            overTickRemainTime -= Time.deltaTime;
            if (overTickRemainTime > 0)
            {
                over = overTickRemainTime;
            }
            else
            {
                overTicking = false;
                overTickRemainTime = 0;
            }
        }

        if (prevSec != sec)
        {
            overTicking = true;
            overTickRemainTime = overTickTimeMax;
        }


        if (secondHand != null) secondHand.transform.localEulerAngles =
            new Vector3(0, 0, -6.0f * sec + -4.0f * over);
        prevSec = sec;
    }

    private void Update()
    {
        DateTime dtNow = DateTime.Now;

        int min = dtNow.Minute;
        int hour = dtNow.Hour;
        int sec = dtNow.Second;

        Clock_Tick(min, hour, sec);

        //時間帯判定A,Bが入る(A..通常 B..フェードアウト用)
        //時間帯判定A,Bで出てきた値に応じて、musicかskyboxの切り替え処理を行う

        //処理ある？    A異   B異
        //         音   ○　　○
        //         空   ○    --


        int day = dtNow.Day;

        if (day != prevDay)
        {
            RecalcSwitchingDT(dtNow);
            prevDay = day;
        }

        if (controlMusic || controlSkybox)
        {
            //定刻の検出
            int cur_dt = DtOfThisPos[CurrentDTGeneral(dtNow, switchingDTs)];
            if (cur_dt != prevDT)
            {
                prevDT = cur_dt;
                if (controlSkybox) RefreshSky();
                if (controlMusic) SwitchAudioFadeStat(AUDIO_FADING_IN);
            }
        }


        if (controlMusic)
        {
            //定刻接近の検出
            int cur_dt_appr = DtPrevOfThisPos[CurrentDTGeneral(dtNow, fadeoutStartDTs)];
            if (cur_dt_appr != prevDT_Audio)
            {
                prevDT_Audio = cur_dt_appr;
                if (!thisisFirstFade) SwitchAudioFadeStat(AUDIO_FADING_OUT);
                thisisFirstFade = false;
            }

            //窓確認用
            chk_str = "DT[" + prevDT.ToString() + "] DT_A[" + prevDT_Audio.ToString() + "]";

            //経時音量制御
            float vol = 0f;
            switch (currentAudioStat)
            {
                case AUDIO_FADING_IN:
                    chk_str += " AUDIO_FADING_IN";
                    audioRemainFadeTime -= Time.deltaTime;
                    vol = 1 - audioRemainFadeTime * fadeInMax_INV;
                    if (audioRemainFadeTime < 0)
                    {
                        audioRemainFadeTime = 0;
                        currentAudioStat = AUDIO_PLAYING;
                    }
                    break;

                case AUDIO_FADING_OUT:
                    chk_str += " AUDIO_FADING_OUT";　
                    audioRemainFadeTime -= Time.deltaTime;
                    vol = audioRemainFadeTime * fadeOutMax_INV;
                    if (audioRemainFadeTime < 0)
                    {
                        audioRemainFadeTime = 0;
                        currentAudioStat = AUDIO_IDLE;
                    }
                    break;

                case AUDIO_IDLE:
                    chk_str += " AUDIO_IDLE";
                    //vol = 0;
                    break;

                case AUDIO_PLAYING:
                    chk_str += " AUDIO_PLAYING";
                    vol = 1;
                    break;
            }

            chk_vol = vol;
            if (audioSrc != null) audioSrc.volume = vol;

            chk_rem = audioRemainFadeTime;


        }


    }

    private void SwitchAudioFadeStat(int mode)
    {
        switch (mode)
        {
            case AUDIO_FADING_IN:

                if (audioSrc != null && switchingClips[prevDT] != null)
                {
                    audioSrc.clip = switchingClips[prevDT];
                    audioSrc.Play();
                }
                audioRemainFadeTime = fadeInMax;
                currentAudioStat = AUDIO_FADING_IN;
                break;

            case AUDIO_FADING_OUT:

                audioRemainFadeTime = fadeOutMax;
                currentAudioStat = AUDIO_FADING_OUT;
                break;

        }
    }

    private void RefreshSky()
    {
        //skybox
        if (switchingMats[prevDT] != null)
        {
            RenderSettings.skybox = switchingMats[prevDT];
        }
        if (gmObjCloudsea != null)
        {
            MeshRenderer mesh = gmObjCloudsea.GetComponent<MeshRenderer>();
            mesh.material = switchingClouds[prevDT];
        }

        //directionalLights
        for (int i = 0; i < switchingLights.Length; i++)
        {
            if (i == prevDT) continue;
            if (switchingLights[i] != null) switchingLights[i].SetActive(false);
        }

        if (switchingLights[prevDT] != null)
            switchingLights[prevDT].SetActive(true);
    }

    private int CurrentDTGeneral(DateTime dtNow, DateTime[] DTs)
    {
        int pass_count = 0;

        for (int i = 0; i < DTs.Length; i++)
        {
            if (dtNow > DTs[i]) pass_count++;
        }

        if (pass_count != 0) pass_count--;
        if (pass_count == 0) pass_count = 3;

        return pass_count;
    }

}
