
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System;
using UnityEngine.UI;

public class clock2_manager : UdonSharpBehaviour
{
    //-----------------------------------


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

    [Header("開始時刻 / 昼")]
    [SerializeField, Range(0, 23)] int dayHour = 7;
    [SerializeField, Range(0, 59)] int dayMin = 00;
    [SerializeField] Material skyboxDay;
    [SerializeField] Material cloudseaDay;
    [SerializeField] AudioClip audioDay;

    [Header("開始時刻 / 夕方")]
    [SerializeField, Range(0, 23)] int eveningHour = 17;
    [SerializeField, Range(0, 59)] int eveningMin = 00;
    [SerializeField] Material skyboxEvening;
    [SerializeField] Material cloudseaEvening;
    [SerializeField] AudioClip audioEvening;

    [Header("開始時刻 / 夜")]
    [SerializeField, Range(0, 23)] int nightHour = 19;
    [SerializeField, Range(0, 59)] int nightMin = 00;
    [SerializeField] Material skyboxNight;
    [SerializeField] Material cloudseaNight;
    [SerializeField] AudioClip audioNight;

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

    string[] stDateTime = new string[] { "Morning", "Day", "Evening", "Night" };

    private int prevDT = -1;
    private int prevDT_Audio = -1;

    [Header("秒針拡張(触らないことをお勧めします)")]
    [SerializeField] bool overTickEnabled = true;

    //[Header("検査用")]
    //[SerializeField] Text logText;

    void Start()
    {
        fadeInMax = fadeInTime;
        fadeOutMax = fadeOutTime;

        switchingMats = new Material[] {
            skyboxMorning, skyboxDay, skyboxEvening, skyboxNight
        };

        switchingClouds = new Material[] {
            cloudseaMorning, cloudseaDay, cloudseaEvening, cloudseaNight
        };

        switchingClips = new AudioClip[] {
            audioMorning, audioDay, audioEvening, audioNight
        };
        //-------------------------------------------

        //lightMapData0 = LightmapSettings.lightmaps; // デフォルトのライトマップをlightMapData0に入れる

        //lightMapData1 = new LightmapData[1]; // 空のLightmapData型の配列を作る。
        //lightMapData1[0] = new LightmapData(); // 1つめの要素にLightmapData型のインスタンスを作成する
        //lightMapData1[0].lightmapColor = lightMap[0]; // ライトマップを設定

    }

    private void RecalcSwitchingDT()
    {
        switchingDTs = new DateTime[]
        {
            new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day, morningHour,morningMin,0),

            new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day, dayHour,dayMin,0),

            new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,eveningHour,eveningMin,0),

            new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day, nightHour,nightMin,0),
        };

        //2は発動用マージン
        TimeSpan ts_fadeout = new TimeSpan(0, 0, fadeOutTime + 2);


        fadeoutStartDTs = new DateTime[]
        {
            switchingDTs[0] - ts_fadeout,
            switchingDTs[1] - ts_fadeout,
            switchingDTs[2] - ts_fadeout,
            switchingDTs[3] - ts_fadeout,
        };
        //for (int i = 0; i < switchingDTs.Length; i++)
        //{
        //    DateTime dateTime = switchingDTs[i] - ts_fadeout;
        //    fadeoutStartDTs[i] = dateTime;
        //}
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



        //if (Input.GetKeyDown(UnityEngine.KeyCode.A))
        //{
        //    LightmapSettings.lightmaps = lightMapData0;
        //    LightmapSettings.lightProbes = lightProbe[0];
        //    probeComponent.customBakedTexture = reflectionProbe[0];
        //}

        //if (Input.GetKeyDown(UnityEngine.KeyCode.B))
        //{
        //    LightmapSettings.lightmaps = lightMapData1;
        //    LightmapSettings.lightProbes = lightProbe[1];
        //    probeComponent.customBakedTexture = reflectionProbe[1];
        //}

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
        RenderSettings.skybox = switchingMats[prevDT];

        if (gmObjCloudsea != null)
        {
            MeshRenderer mesh = gmObjCloudsea.GetComponent<MeshRenderer>();
            mesh.material = switchingClouds[prevDT];
        }


    }




}
