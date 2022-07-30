
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System;
using UnityEngine.UI;

public class clock2_manager : UdonSharpBehaviour
{
    //-----------------------------------

    //[SerializeField] 

    [SerializeField] GameObject shortHand;
    [SerializeField] GameObject longHand;
    [SerializeField] GameObject secondHand;

    [SerializeField] AudioSource audioSrc;

    [Header("この時計でこのワールドのSkyboxを制御する")]
    [SerializeField] bool controlSkybox = false;

    [Header("この時計でこのワールドのBGMを制御する")]
    [SerializeField] bool controlMusic = false;
    [SerializeField, Range(0, 25)] int fadeInTime = 10;
    [SerializeField, Range(0, 25)] int fadeOutTime = 15;

    [Header("Cloud Sea")]
    [SerializeField] GameObject gmObjCloudsea;

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

    //private const int MODE_SKY = 31;
    //private const int MODE_AUDIO = 32;

    private float overTickRemainTime = 0.0f;
    private float overTickTimeMax = 0.2f;
    private bool overTicking = false;
    private int prevSec = -1;

    private float fadeInMax;
    private float fadeOutMax;
    private float currentVol;

    private int[] startsHour;
    private int[] startsMin;

    private int prevDay = -1;
    DateTime[] switchingDTs;

    DateTime[] fadeoutStartDTs;


    Material[] switchingMats;
    Material[] switchingClouds;

    AudioClip[] switchingClips;
    GameObject[] switchingLights;

    string[] stDateTime = new string[] { "Morning", "Day", "Evening", "Night" };

    private int prevDT = -1;
    private int prevDT_Audio = -1;

    [Header("秒針拡張(触らないことをお勧めします)")]
    [SerializeField] bool overTickEnabled = true;

    //[Header("検査用")]
    //[SerializeField] Text logText;

    void Start()
    {
        fadeInMax = fadeInTime + 0.1f;
        fadeOutMax = fadeOutTime + 0.1f;

        switchingMats = new Material[] {
            skyboxMorning, skyboxDay, skyboxEvening, skyboxNight
        };

        switchingClouds = new Material[] {
            cloudseaMorning, cloudseaDay, cloudseaEvening, cloudseaNight
        };

        switchingClips = new AudioClip[] {
            audioMorning, audioDay, audioEvening, audioNight
        };

        switchingLights = new GameObject[]
        {
            directionalLightMorning, directionalLightDay, directionalLightEvening, directionalLightNight
        };

        foreach (GameObject gmObj in switchingLights)
        {
            if (gmObj != null) gmObj.SetActive(false);
        }

        //-------------------------------------------

        RecalcSwitchingDT();

    }

    private void RecalcSwitchingDT()
    {
        DateTime dtNow = DateTime.Now;

        switchingDTs = new DateTime[]
        {
            new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, morningHour,morningMin,0),
            new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, dayHour,dayMin,0),
            new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, eveningHour,eveningMin,0),
            new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, nightHour,nightMin,0),
        };

        //2は発動用マージン
        TimeSpan ts_fadeout = new TimeSpan(0, 0, fadeOutTime + 1);

        fadeoutStartDTs = new DateTime[]
        {
            switchingDTs[0] - ts_fadeout,
            switchingDTs[1] - ts_fadeout,
            switchingDTs[2] - ts_fadeout,
            switchingDTs[3] - ts_fadeout,
        };

        for (int d = 0; d < fadeoutStartDTs.Length; d++)
        {
            if (DateTime.Now.Day != switchingDTs[d].Day)
            {
                //同時刻で日付のみ使えるよう更新
                switchingDTs[d] = new DateTime(
                        dtNow.Year, dtNow.Month, dtNow.Day,
                        switchingDTs[d].Hour,
                        switchingDTs[d].Minute,
                        switchingDTs[d].Second
                );
            }
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

    private int currentAudioStat = AUDIO_IDLE;
    private const int AUDIO_IDLE = 0;
    private const int AUDIO_FADING_OUT = 51;
    private const int AUDIO_FADING_IN = 52;
    private const int AUDIO_PLAYING = 53;
    private float audioRemainFadeTime = 0;
    private bool firstFadeFlag = true;

    private void Update()
    {
        DateTime dtNow = DateTime.Now;

        int min = dtNow.Minute;
        int hour = dtNow.Hour;
        int sec = dtNow.Second;

        Clock_Tick(min, hour, sec);


        if (controlMusic)
        {
            float vol = 0f;

            switch (currentAudioStat)
            {
                case AUDIO_FADING_IN:
                    audioRemainFadeTime -= Time.deltaTime;
                    vol = 1 - audioRemainFadeTime / fadeInMax;
                    if (audioRemainFadeTime < 0)
                    {
                        audioRemainFadeTime = 0;
                        currentAudioStat = AUDIO_PLAYING;
                    }
                    break;
                case AUDIO_FADING_OUT:
                    audioRemainFadeTime -= Time.deltaTime;
                    vol = audioRemainFadeTime / fadeOutMax;
                    if (audioRemainFadeTime < 0)
                    {
                        audioRemainFadeTime = 0;
                        currentAudioStat = AUDIO_IDLE;
                    }
                    break;
                case AUDIO_IDLE:
                    //RefreshMusic(AUDIO_FADING_IN);
                    break;
                case AUDIO_PLAYING:
                    vol = 1;
                    break;
            }

            if (audioSrc != null)
            {
                audioSrc.volume = vol;
            }

        }


        if (controlMusic || controlSkybox)
        {

            int day = dtNow.Day;

            if (day != prevDay)
            {
                RecalcSwitchingDT();
                prevDay = day;
            }

        }


        if (controlMusic || controlSkybox)
        {

            int currentDT = 3;
            int currentDT_Audio = 3;

            //現在時刻と次の時刻判定
            for (int i = 0; i < switchingDTs.Length; i++)
            {
                if (dtNow < switchingDTs[i])
                {
                    currentDT = (3 + i) % 4;
                    break;
                }
            }

            //fadeoutぶんだけ前のバージョン
            for (int i = 0; i < fadeoutStartDTs.Length; i++)
            {
                if (dtNow < fadeoutStartDTs[i])
                {
                    currentDT_Audio = (3 + i) % 4;
                    break;
                }
            }


            //1フレ前の時間帯と異なる
            if (prevDT != currentDT)
            {
                prevDT = currentDT;
                if (controlSkybox) RefreshSkybox();

                if (controlMusic) RefreshMusic(AUDIO_FADING_IN);
            }


            //音声フェードアウト用
            if (prevDT_Audio != currentDT_Audio)
            {
                prevDT_Audio = currentDT_Audio;
                if (!firstFadeFlag && controlMusic) RefreshMusic(AUDIO_FADING_OUT);
                firstFadeFlag = false;

            }

        }


    }
    private void RefreshMusic(int mode)
    {
        if (audioSrc != null)
        {
            switch (mode)
            {
                case AUDIO_FADING_IN:
                    audioRemainFadeTime = fadeInMax;
                    audioSrc.clip = switchingClips[prevDT];
                    audioSrc.Play();
                    audioRemainFadeTime = fadeInMax;
                    currentAudioStat = AUDIO_FADING_IN;
                    break;
                case AUDIO_FADING_OUT:
                    audioRemainFadeTime = fadeOutMax;
                    currentAudioStat = AUDIO_FADING_OUT;
                    break;

            }
        }

    }

    private void RefreshSkybox()
    {
        //skybox
        RenderSettings.skybox = switchingMats[prevDT];

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

}
