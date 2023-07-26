using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.PUN;
using Cinemachine;
using TMPro;
using TT.Inventory;
using TT.SaveLoadSystem;
using TT.Key;
using TT.Visibility;
using Photon.Voice;
using DG.Tweening;
using System;
using UnityEngine.Animations.Rigging;
using UnityEngine.Rendering;
using TT.Stats;
using TT.Customization;
using Photon.Voice.Unity;
using static AI;

public class PlayerController : MonoBehaviour
{
    //Player Photon Values
    [HideInInspector] public PhotonView me;
    [HideInInspector] public int id;
    [HideInInspector] public int queue;

    public bool killing;
    public bool aconite;
    public bool jump;

    //Player Private Objects
    private GameObject arm, body;
    [HideInInspector] public GameObject voteText, nameText, voiceImage;
    private Animator anim;
    private CinemachineVirtualCamera cineCam;
    private MasterClient mcMain;
    private GameObject myPlayer;

    //Player Private Values
    private float animRotationX, animRotationY;
    private float rotationX, rotationY;
    private float speed;
    private float werewolfCoolDown;
    private Rigidbody rb;
    private bool wwFoodScreenScale;
    private bool zipline;
    private Tweener tween;
    public float slowly;
    public float aconiteTime;
    public float coolDown;
    public bool walking, walkingAudio;
    private bool invertedXAxis, invertedYAxis;

    public bool werewolf;
    public bool ghost;
    public bool isFinish;
    private bool ragdoll;
    [HideInInspector]public bool dead;

    public float surviveTime;

    private int barCount;

    public GameObject lastItem, lastMainItem, bulletLoc;

    //Player Public Values
    [Header("Values")]
    public string nick;
    public float health;
    public float baseSpeed;
    public float sensitivity;
    public int werewolfFood;
    public GameObject bloodEffect;
    public GameObject explosionEffect;
    public Vector2 revolverBulletAmount;
    public Vector2 shotgunBulletAmount;
    public Vector2 silverBulletAmount;
    public AudioSource gunAudio;
    public AudioSource myAudio;
    public AudioSource mouthAudio;
    public AudioSource eatAudio;
    public AudioSource ziplineAudio;
    public AudioSource allAuido;
    private float gasCoolDown;
    private float ragdollCoolDown;
    public GameObject myObject;
    public GameObject windParticle;
    public GameObject ziplineLink;
    private GameObject myZipline;
    private GameObject myLink;
    public GameObject myLinkConnectPoint;
    public Recorder recorder;
    private bool voiceState;

#if UNITY_EDITOR
    private bool JumpTest;
#endif

    [Space(10)] 
    public Item item;

    [Space(10)]
    public Classes role;

    //Player Public Objects
    [Header("Objects")] 
    public GameObject mainArm;
    public Transform head;
    public Transform spineTarget, target, spineBone, headBone;
    public Transform mouth;
    public List<int> values;

    void Awake()
    {
        me = GetComponent<PhotonView>();
        id = me.ViewID;
        anim = transform.GetChild(0).GetComponent<Animator>();
        nick = me.Owner.NickName;
    }

    // Start is called before the first frame update
    void Start()
    {
        Starting();
        allAuido = NetworkingManager.Instance.GetComponent<AudioSource>();
        NetworkingActor.Instance._counter.SetActive(false);

        if (werewolf)
        {
            mouthAudio.clip = NetworkingActor.Instance.wwChanging2;
            mouthAudio.Play();

            allAuido.clip = NetworkingActor.Instance.wwAll;
            allAuido.Play();
        }

        if (me.IsMine)
        {
            for (int i = 0; i < NetworkingManager.Instance.ids.Length; i++)
            {
                if (NetworkingManager.Instance.nicks[i] == nick)
                {
                    me.RPC("ChangeSkin", RpcTarget.All, id, NetworkingManager.Instance.skins[i]);
                }
            }
            GameManager.Instance.isMe = gameObject;

            if (GameManager.Instance.totalPlayer <= 0)
                GameManager.Instance.totalPlayer = PhotonNetwork.CurrentRoom.PlayerCount;

            arm.transform.parent.SetParent(Camera.main.transform);
            ExecuteWithDelay(0.1f, () =>
            {
                arm.transform.parent.localPosition = new Vector3(0, -0.19f, 0);
                arm.transform.parent.localEulerAngles = new Vector3(0, 0, 0);
            });

            if (PhotonNetwork.IsMasterClient && !GetComponent<MasterClient>())
            {
                ExecuteWithDelay(5f, () =>
                {
                    me.RPC("TakeWWCount", RpcTarget.All, RoomSettings.instance.wwCount);
                });
                gameObject.AddComponent<MasterClient>();
            }

            Transform[] bodyObjs = body.transform.GetComponentsInChildren<Transform>();
            foreach (var obj in bodyObjs)
            {
                if (obj.gameObject.layer == 7)
                {
                    //obj.gameObject.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                }
            }
            Transform[] armObjs = arm.transform.GetComponentsInChildren<Transform>();
            foreach (var obj in armObjs)
            {
                if (obj.gameObject.layer == 8)
                {
                    obj.gameObject.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
            nameText.gameObject.SetActive(false);
            voteText.gameObject.SetActive(false);

            cineCam.m_Follow = head;
            //cineCam.m_LookAt = target;

            GameManager.Instance.isGamePaused = false;

            MissionsManager.Instance.DescControl();
            NetworkingActor.Instance.cineCam.transform.eulerAngles = new Vector3(-animRotationY, transform.localEulerAngles.y, 0);
        }
        else
        {
            arm.SetActive(false);
            voteText.gameObject.SetActive(false);

            if (werewolf)
                nameText.SetActive(false);

            nameText.transform.GetChild(0).GetComponent<TextMeshPro>().text = me.Owner.NickName;

            if (gameObject.layer == 11)
                ghost = true;

            if (ghost)
            {
                if (GameManager.Instance.isMe.GetComponent<PlayerController>().ghost)
                {
                    Visibility.VisibiltyOn();
                    nameText.gameObject.SetActive(true);
                }
                else
                {
                    Visibility.VisibiltyOff();
                    nameText.gameObject.SetActive(false);
                }
            }

            if (werewolf)
            {
                AudioSource hrrAudio = transform.Find("HrrAudio").GetComponent<AudioSource>();
                hrrAudio.clip = NetworkingActor.Instance.hrrAudio;
                hrrAudio.Play();
            }
        }
    }

    void FixedUpdate()
    {
        if (!NetworkingManager.Instance.isRoleStarted || GameManager.Instance.isFinish && !ghost)
            return;

        if (GetAnimatorBool("isWalking") || GetAnimatorBool("isRunning"))
        {
            if (!walkingAudio && isGround() && !ghost)
            {
                StartCoroutine(WalkingAudio());
                walkingAudio = true;
            }
        }
        if (jump && isGround() && !dead)
        {
            jump = false;
            me.RPC("SendAudio", RpcTarget.All, id, "drop");
        }

        if (me.IsMine)
        {
            //arm.transform.parent.position = Camera.main.transform.position;
            if (!dead && !killing)
            {
                if (!ragdoll)
                {
                    #region MOVEMENT

                    if (Input.GetKey(KeyManager.Instance.FindKey(KeyNames.Move_Forward)))
                    {
                        if (Input.GetKey(KeyCode.LeftShift))
                        {
                            AchievementManager.Instance.SetAchievement("Runner", Time.deltaTime * speed * 2f);

                            if (Input.GetKey(KeyManager.Instance.FindKey(KeyNames.Move_Left)))
                            {
                                var vectorSpeed3 = transform.forward * speed * 2;
                                var vectorSpeed2 = -transform.right * speed;
                                var vectorSpeed = vectorSpeed2 + vectorSpeed3;
                                rb.velocity = new Vector3(vectorSpeed.x, rb.velocity.y, vectorSpeed.z);
                            }
                            else if (Input.GetKey(KeyManager.Instance.FindKey(KeyNames.Move_Right)))
                            {
                                var vectorSpeed3 = transform.forward * speed * 2;
                                var vectorSpeed2 = transform.right * speed;
                                var vectorSpeed = vectorSpeed2 + vectorSpeed3;
                                rb.velocity = new Vector3(vectorSpeed.x, rb.velocity.y, vectorSpeed.z);
                            }
                            else
                            {
                                var vectorSpeed = transform.forward * speed * 2;
                                rb.velocity = new Vector3(vectorSpeed.x, rb.velocity.y, vectorSpeed.z);
                            }
                            if (werewolf)
                            {
                                SetAnimatorBool("isRunning", true);
                                SetAnimatorBool("isWalking", false);
                            }
                            if (isGround() && !werewolf)
                            {
                                SetAnimatorBool("isRunning", true);
                                SetAnimatorBool("isWalking", false);
                            }
                        }
                        else
                        {
                            AchievementManager.Instance.SetAchievement("Runner", Time.deltaTime * speed);
                            if (Input.GetKey(KeyManager.Instance.FindKey(KeyNames.Move_Left)))
                            {
                                var vectorSpeed = (-transform.right + transform.forward) * speed;
                                rb.velocity = new Vector3(vectorSpeed.x, rb.velocity.y, vectorSpeed.z);
                            }
                            else if (Input.GetKey(KeyManager.Instance.FindKey(KeyNames.Move_Right)))
                            {
                                var vectorSpeed = (transform.right + transform.forward) * speed;
                                rb.velocity = new Vector3(vectorSpeed.x, rb.velocity.y, vectorSpeed.z);
                            }
                            else
                            {
                                var vectorSpeed = transform.forward * speed;
                                rb.velocity = new Vector3(vectorSpeed.x, rb.velocity.y, vectorSpeed.z);
                            }
                            if (isGround())
                            {
                                SetAnimatorBool("isRunning", false);
                                SetAnimatorBool("isWalking", true);
                            }
                        }
                    }
                    else if (Input.GetKey(KeyManager.Instance.FindKey(KeyNames.Move_Backward)))
                    {
                        AchievementManager.Instance.SetAchievement("Runner", Time.deltaTime * speed);

                        if (Input.GetKey(KeyManager.Instance.FindKey(KeyNames.Move_Left)))
                        {
                            var vectorSpeed = (-transform.right + -transform.forward) * speed;
                            rb.velocity = new Vector3(vectorSpeed.x, rb.velocity.y, vectorSpeed.z);
                        }
                        else if (Input.GetKey(KeyManager.Instance.FindKey(KeyNames.Move_Right)))
                        {
                            var vectorSpeed = (transform.right + -transform.forward) * speed;
                            rb.velocity = new Vector3(vectorSpeed.x, rb.velocity.y, vectorSpeed.z);
                        }
                        else
                        {
                            var vectorSpeed = -transform.forward * speed;
                            rb.velocity = new Vector3(vectorSpeed.x, rb.velocity.y, vectorSpeed.z);
                        }
                        if (isGround())
                        {
                            SetAnimatorBool("isRunning", false);
                            SetAnimatorBool("isWalking", true);
                        }
                    }
                    else
                    {
                        SetAnimatorBool("isWalking", false);
                        SetAnimatorBool("isRunning", false);

                        if (Input.GetKey(KeyManager.Instance.FindKey(KeyNames.Move_Left)))
                        {
                            var vectorSpeed = -transform.right * speed;
                            rb.velocity = new Vector3(vectorSpeed.x, rb.velocity.y, vectorSpeed.z);
                        }
                        else if (Input.GetKey(KeyManager.Instance.FindKey(KeyNames.Move_Right)))
                        {
                            var vectorSpeed = transform.right * speed;
                            rb.velocity = new Vector3(vectorSpeed.x, rb.velocity.y, vectorSpeed.z);
                        }
                        else
                        {
                            rb.velocity = Vector3.zero + new Vector3(0, rb.velocity.y, 0);
                        }
                    }

                    if (Input.GetKey(KeyManager.Instance.FindKey(KeyNames.Crouch)))
                    {
                        SetAnimatorBool("isCrouch", true);
                        //RigBuilder builder = transform.GetComponentInChildren<RigBuilder>();
                        //ExecuteWithDelay(0.1f, () => { builder.enabled = false; });
                    }
                    else
                    {
                        SetAnimatorBool("isCrouch", false);
                        //RigBuilder builder = transform.GetComponentInChildren<RigBuilder>();
                        //ExecuteWithDelay(0.1f, () => { builder.enabled = true; });
                    }

                    if (Input.GetKey(KeyManager.Instance.FindKey(KeyNames.Jump)))
                    {
                        if(isGround())
                        {
                            if (!werewolf)
                                SetAnimatorBool("isRunning", false);
                            SetAnimatorBool("isJump", true);
                            SetAnimatorBool("isWalking", false);
                            ExecuteWithDelay(0.4f, () => { jump = true; });
                            ExecuteWithDelay(AnimationLenght("Jump"), () =>
                            {
                                if (me.IsMine)
                                    SetAnimatorBool("isJump", false);
                            });
                            rb.velocity = new Vector3(rb.velocity.x, 10, rb.velocity.z);
                        }
                    }

                    #endregion
                }
            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (me.IsMine)
        {
            #if UNITY_EDITOR
            if(Input.GetKey(KeyCode.O) && Input.GetKey(KeyCode.P) && Input.GetKeyDown(KeyCode.RightShift)) 
            {
                JumpTest = !JumpTest;
            }
            #endif

            sensitivity = SettingsManager.Instance.sensitivity;
            invertedXAxis = SettingsManager.Instance.xAxis;
            invertedYAxis = SettingsManager.Instance.yAxis;
            switch (item.bulletType)
            {
                case BulletType.None:
                    NetworkingActor.Instance.bulletAmount.transform.parent.gameObject.SetActive(false);
                    break;
                case BulletType.mm9:
                    NetworkingActor.Instance.bulletAmount.transform.parent.gameObject.SetActive(true);
                    NetworkingActor.Instance.bulletAmount.text = $"{(int)revolverBulletAmount.x}/{(int)revolverBulletAmount.y}";
                    break;
                case BulletType.Shotgun:
                    NetworkingActor.Instance.bulletAmount.transform.parent.gameObject.SetActive(true);
                    NetworkingActor.Instance.bulletAmount.text = $"{(int)shotgunBulletAmount.x}/{(int)shotgunBulletAmount.y}";
                    break;
                case BulletType.Silver:
                    NetworkingActor.Instance.bulletAmount.transform.parent.gameObject.SetActive(true);
                    NetworkingActor.Instance.bulletAmount.text = $"{(int)silverBulletAmount.x}/{(int)silverBulletAmount.y}";
                    break;
            }
            if (!dead && !killing)
            {
                if (!GameManager.Instance.isFinish)
                    surviveTime += Time.deltaTime;
                #region Ww Rage Screen
                if (role.party == Classes.Party.Evil && werewolf)
                    NetworkingActor.Instance.wwFoodScreen.gameObject.SetActive(false);
                if (role.party == Classes.Party.Village)
                    NetworkingActor.Instance.wwFoodScreen.gameObject.SetActive(false);

                if (!werewolf && role.party == Classes.Party.Evil)
                {
                    NetworkingActor.Instance.wwFoodScreen.gameObject.SetActive(true);
                    Color color = NetworkingActor.Instance.wwFoodScreen.color;
                    NetworkingActor.Instance.wwFoodScreen.color =
                        new Color(color.r, color.g, color.b, (float)werewolfFood / 4f);

                    if (werewolfFood >= 4 && DayManager.Instance.isAnger)
                    {
                        NetworkingActor.Instance.wwRage.transform.GetChild(0).gameObject.SetActive(true);
                        NetworkingActor.Instance.wwRage.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = KeyManager.Instance.FindKey(KeyNames.Changing).ToString();
                        if (wwFoodScreenScale)
                        {
                            NetworkingActor.Instance.wwRage.transform.GetChild(0).localScale += Vector3.one * Time.deltaTime;
                            NetworkingActor.Instance.wwFoodScreen.transform.localScale += Vector3.one * Time.deltaTime / 4f;
                            if (NetworkingActor.Instance.wwFoodScreen.transform.localScale.x >= 1.1f)
                            {
                                wwFoodScreenScale = false;
                            }
                        }
                        else
                        {
                            NetworkingActor.Instance.wwRage.transform.GetChild(0).localScale -= Vector3.one * Time.deltaTime;
                            NetworkingActor.Instance.wwFoodScreen.transform.localScale -= Vector3.one * Time.deltaTime / 4f;
                            if (NetworkingActor.Instance.wwFoodScreen.transform.localScale.x <= 1f)
                            {
                                wwFoodScreenScale = true;
                            }
                        }
                    }
                    else
                        NetworkingActor.Instance.wwRage.transform.GetChild(0).gameObject.SetActive(false);
                }

                if (!werewolf && role.party == Classes.Party.Evil)
                    NetworkingActor.Instance.wwRage.fillAmount = (float)werewolfFood / 4f;
                if (werewolf && role.party == Classes.Party.Evil)
                    NetworkingActor.Instance.wwRage.fillAmount = (float)werewolfCoolDown / 30f;

                NetworkingActor.Instance.wwRage.transform.parent.gameObject.SetActive(role.party == Classes.Party.Evil ? true : false);
                #endregion
            }

            if (slowly > 0)
            {
                speed = baseSpeed / 2f;
                slowly -= Time.deltaTime;
            }
            else if (slowly < 0 && speed == baseSpeed / 2f)
            {
                if (werewolf)
                {
                    speed = baseSpeed * role.speed;
                }
                else
                {
                    speed = baseSpeed;
                }
                slowly = 0f;
            }

            if (aconiteTime > 0)
            {
                aconiteTime -= Time.deltaTime;
            }
            else
            {
                aconiteTime = 0f;
            }

            #region PAUSE

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!GameManager.Instance.isFinish || GameManager.Instance.isFinish && ghost)
                    GameManager.Instance.isGamePaused = !GameManager.Instance.isGamePaused;
            }

            #endregion

            #region PAUSE MENU

            if (GameManager.Instance.isGamePaused)
            {
                Cursor.lockState = CursorLockMode.Confined;
                if (GameManager.Instance.isFinish)
                    NetworkingActor.Instance.winScreen.SetActive(true);
                else
                    NetworkingActor.Instance.settingsPanel.SetActive(true);

                NetworkingActor.Instance.healthBar.transform.parent.parent.gameObject.SetActive(false);
                NetworkingActor.Instance.hotBar.SetActive(false);
                NetworkingActor.Instance.wwFoodScreen.gameObject.SetActive(false);
                NetworkingActor.Instance.voice.gameObject.SetActive(false);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                if (GameManager.Instance.isFinish)
                    NetworkingActor.Instance.winScreen.SetActive(false);
                else
                    NetworkingActor.Instance.settingsPanel.SetActive(false);

                if (ghost)
                {
                    NetworkingActor.Instance.healthBar.transform.parent.parent.gameObject.SetActive(false);
                    NetworkingActor.Instance.hotBar.SetActive(false);
                    NetworkingActor.Instance.wwFoodScreen.gameObject.SetActive(false);
                    NetworkingActor.Instance.voice.gameObject.SetActive(false);
                }
                else
                {
                    NetworkingActor.Instance.healthBar.transform.parent.parent.gameObject.SetActive(true);
                    NetworkingActor.Instance.hotBar.SetActive(true);
                    NetworkingActor.Instance.voice.gameObject.SetActive(true);
                    if (role.party == Classes.Party.Evil)
                        NetworkingActor.Instance.wwFoodScreen.gameObject.SetActive(true);
                }
            }

            #endregion

            #region DAY LOOP
            if (DayManager.Instance.isStarted)
            {
                if (PhotonNetwork.IsMasterClient && GetComponent<PhotonView>().IsMine)
                {
                    DayManager.Instance.timer += Time.deltaTime;
                    anim.SetFloat("timer", DayManager.Instance.timer);

                    if (DayManager.Instance.phase2)
                    {
                        DayManager.Instance.timer2 += Time.deltaTime;
                        anim.SetFloat("timer2", DayManager.Instance.timer2);
                    }

                    if (DayManager.Instance.timer >= DayManager.Instance.p1angerStart && !DayManager.Instance.isAnger)
                    {
                        if (!DayManager.Instance.phase2)
                        {
                            me.RPC("Dongu", RpcTarget.All, "anger");
                        }

                        DayManager.Instance.timer = 0;
                        anim.SetFloat("timer", DayManager.Instance.timer);
                    }
                    if (DayManager.Instance.timer >= DayManager.Instance.p1angerTime && DayManager.Instance.isAnger)
                    {
                        if (!DayManager.Instance.phase2)
                        {
                            me.RPC("Dongu", RpcTarget.All, "night");
                        }
                        me.RPC("GasCloud", RpcTarget.All, 1);
                        DayManager.Instance.timer = 0;
                        anim.SetFloat("timer", DayManager.Instance.timer);
                    }
                    if (DayManager.Instance.timer2 >= DayManager.Instance.p2angerStart && !DayManager.Instance.isAnger)
                    {
                        me.RPC("Dongu", RpcTarget.All, "anger");
                        DayManager.Instance.timer2 = 0;
                        anim.SetFloat("timer2", DayManager.Instance.timer2);
                    }
                    if (DayManager.Instance.timer2 >= DayManager.Instance.p2angerTime && DayManager.Instance.isAnger)
                    {
                        me.RPC("GasCloud", RpcTarget.All, 2);
                        DayManager.Instance.timer2 = 0;
                        anim.SetFloat("timer2", DayManager.Instance.timer2);
                    }
                }
                else if(!PhotonNetwork.IsMasterClient && GetComponent<PhotonView>().IsMine)
                {
                    if (!mcMain) mcMain = GameObject.FindObjectOfType<MasterClient>();
                    else
                    {
                        DayManager.Instance.timer = mcMain.transform.GetChild(0).GetComponent<Animator>().GetFloat("timer");
                        DayManager.Instance.timer2 = mcMain.transform.GetChild(0).GetComponent<Animator>().GetFloat("timer2");
                    }
                }
            }
            #endregion

            #region DEAD
            if (health <= 0 && !dead)
            {
                me.RPC("Dead", RpcTarget.All, me.ViewID, transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.z, transform.position.x, transform.position.y, transform.position.z, false);

                if (GetComponent<PhotonVoiceView>().RecorderInUse.IsRecording)
                {
                    GetComponent<PhotonVoiceView>().RecorderInUse.IsRecording = false;
                }
                NetworkingActor.Instance.settingsPanel.SetActive(false);
            }
            #endregion

            if (!voiceState && !SettingsManager.Instance.pushtotalk)
            {
                if (recorder.LevelMeter.CurrentAvgAmp >= 0.01f)
                {
                    me.RPC("SetVoiceImage", RpcTarget.All, id, true);
                    NetworkingActor.Instance.voice.color = new Color(1f, 1f, 1f, 1f);
                    voiceState = true;
                }
            }
            if (voiceState && !SettingsManager.Instance.pushtotalk)
            {
                if (recorder.LevelMeter.CurrentAvgAmp < 0.01f)
                {
                    me.RPC("SetVoiceImage", RpcTarget.All, id, false);
                    if (!SettingsManager.Instance.pushtotalk)
                    {
                        NetworkingActor.Instance.voice.color = new Color(1f, 1f, 1f, 0.15f);
                    }
                    voiceState = false;
                }
            }
        }

        if (!NetworkingManager.Instance.isRoleStarted || GameManager.Instance.isGamePaused)
            return;

        animRotationX = anim.GetFloat("rotationX");
        animRotationY = anim.GetFloat("rotationY");

        target.position = spineTarget.transform.GetChild(0).position;

        coolDown -= Time.deltaTime;

        if (health <= 0)
            health = 0;

        if (me.IsMine)
        {
            if (!werewolf)
                NetworkingActor.Instance.healthBar.fillAmount = health / 100f;
            else
                NetworkingActor.Instance.healthBar.fillAmount = health / role.health;

            head.transform.position = new Vector3(head.transform.position.x, headBone.transform.position.y, head.transform.position.z);
            spineTarget.transform.position = new Vector3(spineTarget.transform.position.x,
                headBone.transform.position.y + 0.3f, spineTarget.transform.position.z);
            NetworkingActor.Instance.cineCam.transform.eulerAngles = new Vector3(-animRotationY, transform.localEulerAngles.y, 0);

            if (werewolf)
                Camera.main.GetComponent<Volume>().profile = NetworkingActor.Instance.ww;
            else if (ghost)
                Camera.main.GetComponent<Volume>().profile = NetworkingActor.Instance.ghost;
            else
                Camera.main.GetComponent<Volume>().profile = NetworkingActor.Instance.normal;

            if (!dead && !ghost)
            {
                if (!DayManager.Instance.phase2 && DayManager.Instance.p1Gas)
                {
                    NetworkingActor.Instance.gasSound.SetActive(true);
                    gasCoolDown += Time.deltaTime;
                    if (gasCoolDown >= 4f)
                    {
                        gasCoolDown = 0f;
                        if (!werewolf)
                            me.RPC("Hit", RpcTarget.All, id, 10f);
                        else
                            me.RPC("Hit", RpcTarget.All, id, 60f);
                    }
                }
                else if (DayManager.Instance.phase2 && DayManager.Instance.p2Gas)
                {
                    NetworkingActor.Instance.gasSound.SetActive(true);
                    gasCoolDown += Time.deltaTime;
                    if (gasCoolDown >= 4f)
                    {
                        gasCoolDown = 0f;
                        if (!werewolf)
                            me.RPC("Hit", RpcTarget.All, id, 10f);
                        else
                            me.RPC("Hit", RpcTarget.All, id, 60f);
                    }
                }
                else
                {
                    NetworkingActor.Instance.gasSound.SetActive(false);
                }
            }
            else
            {
                if (mouthAudio.clip != null)
                {
                    me.RPC("StopAudio", RpcTarget.All, id, "cough");
                }
            }

            if (role.party == Classes.Party.Evil)
            {
                if (GameManager.Instance.deadPlayer + GameManager.Instance.escapedPlayer + GameManager.Instance.quitPlayer >= GameManager.Instance.totalPlayer - RoomSettings.instance.wwCount)
                {
                    if (GameManager.Instance.escapedPlayer > 0)
                    {
                        Stats.Lose(surviveTime);
                    }
                    else
                    {
                        Stats.Win(surviveTime);
                    }
                }
            }

            if (!dead && !killing)
            {

                if (!ragdoll)
                {
                    #region ARM MOVEMENT

                    if (animRotationX != 0)
                    {
                        spineTarget.transform.localEulerAngles = new Vector3(-animRotationY, animRotationX, 0);
                        arm.transform.parent.transform.localEulerAngles = Camera.main.transform.rotation.eulerAngles;
                    }
                    else
                    {
                        spineTarget.transform.localEulerAngles = new Vector3(-animRotationY, 0, 0);
                        //arm.transform.parent.transform.localEulerAngles = new Vector3(Camera.main.transform.rotation.eulerAngles.x, 0, 0);
                        //arm.transform.parent.LookAt(target.transform.position);
                    }

                    #endregion

                    #region CAMERA

                    if (Cursor.lockState == CursorLockMode.Locked)
                    {
                        rotationY += Input.GetAxis("Mouse Y") * sensitivity * (invertedYAxis ? -1 : 1);
                        rotationY = Mathf.Clamp(rotationY, -75, 45);
                        anim.SetFloat("rotationY", rotationY);

                        rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivity * (invertedXAxis ? -1 : 1);
                        transform.localEulerAngles = new Vector3(0, rotationX, 0);
                        anim.SetFloat("rotationX", 0);
                    }

                    #endregion

                    if (!ghost)
                    {
                        #region ATTACK
                        if (Input.GetKeyDown(KeyManager.Instance.FindKey(KeyNames.Attack)))
                        {
                            if(item.itemType == ItemType.Food && !GetAnimatorBool("isEating"))
                            {
                                Inventory.DecreaseItem(barCount, 1);
                                me.RPC("SendAudio", RpcTarget.All, id, "eat");
                                SetAnimatorBool("isEating", true);

                                AchievementManager.Instance.SetAchievement("Appetite");

                                ExecuteWithDelay(3f, () =>
                                {
                                    SetAnimatorBool("isEating", false);
                                    health += item.health;
                                    if (health > 100)
                                        health = 100;
                                    GetComponent<PhotonView>().RPC("HotBar", RpcTarget.All, id, barCount, InventoryManager.hotBarMeta[barCount - 1].name, InventoryManager.hotBarMeta[barCount - 1].count);
                                });
                            }
                            me.RPC("SetBulletAmounts", RpcTarget.All, id, shotgunBulletAmount.x, shotgunBulletAmount.y, revolverBulletAmount.x, revolverBulletAmount.y, silverBulletAmount.x, silverBulletAmount.y);
                            if (!GetAnimatorBool("isAttack") && !GetAnimatorBool("isReloading") && !werewolf && item.animationIdle != "Food")
                            {
                                RaycastHit hits;
                                if (Physics.Raycast(Camera.main.transform.position + Camera.main.transform.forward * 2f, Camera.main.transform.forward, out hits, Mathf.Infinity, LayerMask.GetMask("Hit")))
                                {
                                    switch (item.bulletType)
                                    {
                                        case BulletType.mm9:
                                            if (anim.GetCurrentAnimatorStateInfo(2).IsName("Idle_Revolver") || anim.GetCurrentAnimatorStateInfo(2).IsName("Walk") || anim.GetCurrentAnimatorStateInfo(2).IsName("Running"))
                                            {
                                                me.RPC("Attack", RpcTarget.All, id, bulletLoc == null ? Vector3.zero : bulletLoc.transform.position, hits.point);
                                            }
                                            break;
                                        case BulletType.Shotgun:
                                            if (anim.GetCurrentAnimatorStateInfo(2).IsName("Idle_Rifle") || anim.GetCurrentAnimatorStateInfo(2).IsName("Walk") || anim.GetCurrentAnimatorStateInfo(2).IsName("Running"))
                                            {
                                                me.RPC("Attack", RpcTarget.All, id, bulletLoc == null ? Vector3.zero : bulletLoc.transform.position, Camera.main.transform.position + Camera.main.transform.forward * 50f);
                                            }
                                            break;
                                        case BulletType.Silver:
                                            if (anim.GetCurrentAnimatorStateInfo(2).IsName("Idle_Special") || anim.GetCurrentAnimatorStateInfo(2).IsName("Walk") || anim.GetCurrentAnimatorStateInfo(2).IsName("Running"))
                                            {
                                                me.RPC("Attack", RpcTarget.All, id, bulletLoc == null ? Vector3.zero : bulletLoc.transform.position, hits.point);
                                            }
                                            break;
                                        case BulletType.None:
                                            if (anim.GetCurrentAnimatorStateInfo(2).IsName("Idle") || anim.GetCurrentAnimatorStateInfo(2).IsName("Walk") || anim.GetCurrentAnimatorStateInfo(2).IsName("Running"))
                                            {
                                                me.RPC("Attack", RpcTarget.All, id, bulletLoc == null ? Vector3.zero : bulletLoc.transform.position, Camera.main.transform.position + Camera.main.transform.forward * 50f);
                                            }
                                            break;
                                    }
                                }
                                else
                                {
                                    if (anim.GetCurrentAnimatorStateInfo(2).IsName("Idle") || anim.GetCurrentAnimatorStateInfo(2).IsName("Walk") || anim.GetCurrentAnimatorStateInfo(2).IsName("Running") || anim.GetCurrentAnimatorStateInfo(2).IsName("Crouch") ||
                                        anim.GetCurrentAnimatorStateInfo(2).IsName("Idle_Special") || anim.GetCurrentAnimatorStateInfo(2).IsName("Idle_Rifle") || anim.GetCurrentAnimatorStateInfo(2).IsName("Idle_Revolver"))
                                        me.RPC("Attack", RpcTarget.All, id, bulletLoc == null ? Vector3.zero : bulletLoc.transform.position, Camera.main.transform.position + Camera.main.transform.forward * 50f);
                                }
                            }
                        }
                        #endregion

                        #region RELOAD
                        if (Input.GetKey(KeyManager.Instance.FindKey(KeyNames.Reload)))
                        {
                            if (!GetAnimatorBool("isAttack"))
                            {
                                if (item.bulletType == BulletType.mm9 && revolverBulletAmount.x < item.maxBulletInMagazine && revolverBulletAmount.y > 0 ||
                                    item.bulletType == BulletType.Shotgun && shotgunBulletAmount.x < item.maxBulletInMagazine && shotgunBulletAmount.y > 0 ||
                                    item.bulletType == BulletType.Silver && silverBulletAmount.x < item.maxBulletInMagazine && silverBulletAmount.y > 0)
                                {
                                    SetAnimatorBool("isReloading", true);

                                    if (item.bulletType == BulletType.Shotgun)
                                    {
                                        if (shotgunBulletAmount.x < 1f)
                                            me.RPC("SendAudio", RpcTarget.All, id, "shotgun2Bullet");
                                        else
                                            me.RPC("SendAudio", RpcTarget.All, id, "shotgun1Bullet");
                                    }
                                    if (item.bulletType == BulletType.mm9)
                                    {
                                        me.RPC("SendAudio", RpcTarget.All, id, "revolver");
                                    }
                                    if (item.bulletType == BulletType.Silver)
                                    {
                                        me.RPC("SendAudio", RpcTarget.All, id, "special");
                                    }

                                    AnimationClip[] clips = arm.GetComponent<Animator>().runtimeAnimatorController.animationClips;
                                    foreach (AnimationClip clip in clips)
                                    {
                                        if (clip.name == (item.bulletType == BulletType.mm9 ? "Reload_Revolver" : (item.bulletType == BulletType.Shotgun ? (shotgunBulletAmount.x < 1f ? "Reload_Rifle" : "Reload_Rifle2") : "Reload_Special")))
                                        {
                                            float animationLength = clip.length;
                                            if (clip.name == "Reload_Rifle" || clip.name == "Reload_Rifle2")
                                            {
                                                animationLength /= 1.5f;
                                            }

                                            ExecuteWithDelay(animationLength, () =>
                                            {
                                                SetAnimatorBool("isReloading", false);
                                                me.RPC("Reload", RpcTarget.All, id);
                                            });
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        #endregion
                    }
                }

                if (!ghost)
                {
                    #region VOICE CHAT

                    if (SettingsManager.Instance.pushtotalk)
                    {
                        if (Microphone.devices.Length > 0)
                        {
                            if (Input.GetKeyDown(KeyManager.Instance.FindKey(KeyNames.Voice_Chat)))
                            {
                                GetComponent<PhotonVoiceView>().RecorderInUse.IsRecording = true;
                                NetworkingActor.Instance.voice.color = new Color(1f, 1f, 1f, 1f);
                                me.RPC("SetVoiceImage", RpcTarget.All, id, true);

                            }

                            if (Input.GetKeyUp(KeyManager.Instance.FindKey(KeyNames.Voice_Chat)))
                            {
                                GetComponent<PhotonVoiceView>().RecorderInUse.IsRecording = false;
                                NetworkingActor.Instance.voice.color = new Color(1f, 1f, 1f, 0.15f);
                                me.RPC("SetVoiceImage", RpcTarget.All, id, false);
                            }
                        }
                    }
                    else
                    {
                        if (Microphone.devices.Length > 0)
                        {
                            if (!GetComponent<PhotonVoiceView>().RecorderInUse.IsRecording)
                            {
                                GetComponent<PhotonVoiceView>().RecorderInUse.IsRecording = true;
                            }
                        }
                    }

                    #endregion

                    #region RAGDOLL
                    ragdollCoolDown -= Time.deltaTime;
                    if (Input.GetKeyDown(KeyManager.Instance.FindKey(KeyNames.Ragdoll)) && !werewolf && !zipline)
                    {
                        if (!arm.transform.gameObject.activeSelf)
                        {
                            arm.transform.gameObject.SetActive(true);
                            GetComponent<PhotonView>().RPC("RagdollState", RpcTarget.All, id, false);
                        }
                        else
                        {
                            if (ragdollCoolDown <= 0)
                            {
                                arm.transform.gameObject.SetActive(false);
                                GetComponent<PhotonView>().RPC("RagdollState", RpcTarget.All, id, true);
                                ragdollCoolDown = 1f;
                            }
                        }
                    }

                    #endregion

                    #region HOTBAR
                    if (Input.GetKeyDown(KeyCode.Alpha1))
                    {
                        GetComponent<PhotonView>().RPC("HotBar", RpcTarget.All, id, 1, InventoryManager.hotBarMeta[0].name, InventoryManager.hotBarMeta[0].count);
                    }
                    if (Input.GetKeyDown(KeyCode.Alpha2))
                    {
                        GetComponent<PhotonView>().RPC("HotBar", RpcTarget.All, id, 2, InventoryManager.hotBarMeta[1].name, InventoryManager.hotBarMeta[1].count);
                    }
                    if (Input.GetKeyDown(KeyCode.Alpha3))
                    {
                        GetComponent<PhotonView>().RPC("HotBar", RpcTarget.All, id, 3, InventoryManager.hotBarMeta[2].name, InventoryManager.hotBarMeta[2].count);
                    }
                    if (Input.GetKeyDown(KeyCode.Alpha4))
                    {
                        GetComponent<PhotonView>().RPC("HotBar", RpcTarget.All, id, 4, InventoryManager.hotBarMeta[3].name, InventoryManager.hotBarMeta[3].count);
                    }
                    #endregion

                    #region WEREWOLF
                    if (role.party == Classes.Party.Evil && DayManager.Instance.isAnger)
                    {
                        if (Input.GetKeyDown(KeyManager.Instance.FindKey(KeyNames.Changing)))
                        {
                            if (!werewolf)
                            {
                                if (werewolfFood >= 4)
                                {
                                    if (zipline)
                                    {
                                        rb.isKinematic = false;
                                        zipline = false;
                                        tween.Kill();
                                        SetAnimatorBool("isZipline", false);
                                    }
                                    MissionsManager.Instance.CompleteMission("Shapeshift");
                                    SetAnimatorBool("isChanging", true);
                                    me.RPC("SendAudio", RpcTarget.All, id, "changing");
                                    Invoke("Changing", 1.5f);
                                }
                            }
                        }
                        if (werewolf)
                        {
                            werewolfCoolDown -= Time.deltaTime;
                            if (werewolfCoolDown <= 0f)
                            {
                                MissionsManager.Instance.CompleteMission("Kill Villagers");
                                GameObject player = PhotonNetwork.Instantiate("Player", transform.position, transform.rotation, 0, null);
                                player.GetComponent<PlayerController>().werewolf = false;
                                player.GetComponent<PlayerController>().revolverBulletAmount = revolverBulletAmount;
                                player.GetComponent<PlayerController>().shotgunBulletAmount = shotgunBulletAmount;
                                Destroy(arm.transform.parent.gameObject);
                                PhotonNetwork.Destroy(gameObject);
                            }
                        }
                    }
                    #endregion

                    #region DROP ITEM
                    if (Input.GetKeyDown(KeyManager.Instance.FindKey(KeyNames.Drop_Item)))
                    {
                        if (item.count > 0)
                            me.RPC("DropItem", RpcTarget.All, id, Camera.main.transform.position + Camera.main.transform.forward * 2f, item.name, item.count);
                    }
                    #endregion

                    #region Zipline Close
                    if (zipline)
                    {
                        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
                        {
                            rb.isKinematic = false;
                            zipline = false;
                            tween.Kill();
                            SetAnimatorBool("isZipline", false);
                            myZipline.GetComponent<Zipline>().CloseZipline();
                            me.RPC("ZiplineActive", RpcTarget.All, id, false, 0f);
                            me.RPC("HotBar", RpcTarget.All, id, barCount, item.name, item.count);
                            me.RPC("StopAudio", RpcTarget.All, id, "zipline");
                            windParticle.SetActive(false);
                        }
                    }
                    #endregion

                    #region RAYCAST

                    RaycastHit hit;
                    RaycastHit hit2;
                    if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, 5, LayerMask.GetMask("Interectable")))
                    {
                        GameObject obj = hit.transform.gameObject;
                        if (!werewolf)
                        {
                            if (obj.GetComponent<Crate>())
                            {
                                NetworkingActor.Instance.pressUse.SetActive(true);
                                NetworkingActor.Instance.pressUseText.text = "E";
                                if (Input.GetKeyDown(KeyCode.E))
                                {
                                    AchievementManager.Instance.SetAchievement("Looter");
                                    obj.GetComponent<PhotonView>().RPC("Open", RpcTarget.All, obj.GetComponent<PhotonView>().ViewID);
                                }
                            }
                            if (obj.GetComponent<Food>() && role.party == Classes.Party.Evil && werewolfFood < 4)
                            {
                                NetworkingActor.Instance.pressUse.SetActive(true);
                                NetworkingActor.Instance.pressUseText.text = "E";
                                if (Input.GetKeyDown(KeyCode.E))
                                {
                                    me.RPC("SendAudio", RpcTarget.All, id, "wweat");
                                    obj.GetComponent<PhotonView>().RPC("Eat", RpcTarget.All, obj.GetComponent<PhotonView>().ViewID, me.ViewID);
                                }
                            }
                            if (obj.GetComponent<Food>() && role.party != Classes.Party.Evil)
                            {
                                NetworkingActor.Instance.foodWarning.SetActive(true);
                            }
                            if (obj.GetComponent<Mine>())
                            {
                                NetworkingActor.Instance.pressUse.SetActive(true);
                                NetworkingActor.Instance.pressUseText.text = "E";
                                if (Input.GetKeyDown(KeyCode.E))
                                {
                                    if (Inventory.FindItem("Dynamite"))
                                    {
                                        obj.GetComponent<PhotonView>().RPC("PlaceDynamite", RpcTarget.All, obj.GetComponent<PhotonView>().ViewID);
                                        Inventory.DecreaseItemWithName("Dynamite", 1);
                                        me.RPC("HotBar", RpcTarget.All, id, barCount, InventoryManager.Instance.hotBar[barCount - 1].name, InventoryManager.Instance.hotBar[barCount - 1].count);
                                    }
                                }
                            }
                            if (obj.CompareTag("Zipline"))
                            {
                                NetworkingActor.Instance.pressUse.SetActive(true);
                                NetworkingActor.Instance.pressUseText.text = "E";
                                if (Input.GetKeyDown(KeyCode.E))
                                {
                                    myZipline = obj.transform.parent.gameObject;
                                    myZipline.GetComponent<Zipline>().zipline = true;
                                    me.RPC("ZiplineActive", RpcTarget.All, id, true, myZipline.GetComponent<Zipline>().time);
                                    zipline = true;
                                    SetAnimatorBool("isZiplineStarting", true);
                                    rb.isKinematic = true;
                                    transform.position = obj.transform.position - new Vector3(0, 3.5f, 0);
                                    Vector3[] path = new Vector3[obj.transform.parent.GetChild(3).GetChild(0).GetChild(0).GetComponent<LineRenderer>().positionCount];
                                    obj.transform.parent.GetChild(3).GetChild(0).GetChild(0).GetComponent<LineRenderer>().GetPositions(path);
                                    Vector3[] path2 = new Vector3[obj.transform.parent.GetChild(3).GetChild(0).GetChild(0).GetComponent<LineRenderer>().positionCount - 1];
                                    Vector3[] path3 = new Vector3[obj.transform.parent.GetChild(3).GetChild(0).GetChild(0).GetComponent<LineRenderer>().positionCount - 1];

                                    me.RPC("SendAudio", RpcTarget.All, id, $"zipline{(int)myZipline.GetComponent<Zipline>().time}");

                                    for (int i = 0; i < path2.Length; i++)
                                    {
                                        path2[i] = path[i];
                                        path2[i].y -= 3.5f;
                                    }

                                    GameObject newWindParticle = Instantiate(windParticle, transform.position, Quaternion.identity);
                                    newWindParticle.SetActive(true);
                                    newWindParticle.transform.DOPath(path2, obj.transform.parent.GetComponent<Zipline>().time).SetLookAt(0.01f).SetEase(Ease.Linear).OnComplete(() => { Destroy(newWindParticle.gameObject); });


                                    ExecuteWithDelay(1f, () =>
                                    {
                                        if (zipline)
                                        {
                                            SetAnimatorBool("isZiplineStarting", false);
                                            SetAnimatorBool("isZipline", true);
                                        }
                                    });
                                    tween = transform.DOPath(path2, obj.transform.parent.GetComponent<Zipline>().time).SetEase(Ease.Linear).OnComplete(() =>
                                    {
                                        rb.isKinematic = false;
                                        zipline = false;
                                        SetAnimatorBool("isZipline", false);
                                        myZipline.GetComponent<Zipline>().CloseZipline();
                                        me.RPC("ZiplineActive", RpcTarget.All, id, false, 0f);
                                        windParticle.SetActive(false);
                                    });
                                }
                            }
                        }
                    }
                    else if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit2, 5, LayerMask.GetMask("Objects")))
                    {
                        GameObject obj = hit2.transform.gameObject;
                        if (!werewolf)
                        {
                            if (obj.GetComponent<Objects>())
                            {
                                myObject = obj;
                                NetworkingActor.Instance.pressUse.SetActive(true);
                                NetworkingActor.Instance.pressUseText.text = "E";
                                if (Input.GetKeyDown(KeyCode.E))
                                {
                                    if (obj.GetComponent<Objects>().name.EndsWith("Ammo"))
                                    {
                                        me.RPC("SendAudio", RpcTarget.All, id, "takeitem");
                                    }
                                    else
                                    {
                                        if (!Inventory.Full(obj.GetComponent<Objects>().name))
                                        {
                                            me.RPC("SendAudio", RpcTarget.All, id, "takeitem");
                                        }
                                    }
                                    obj.GetComponent<PhotonView>().RPC("Collect", RpcTarget.All, me.ViewID);

                                    if(obj.GetComponent<Objects>().item.name == "Aconite")
                                    {
                                        me.RPC("HasAconite", RpcTarget.All, id, true);
                                    }
                                }
                            }else
                                myObject = null;
                        }
                    }
                    else if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, 50, LayerMask.GetMask("Interectable")))
                    {
                        if (hit.transform.CompareTag("Door"))
                        {
                            NetworkingActor.Instance.pressUse.SetActive(true);
                            NetworkingActor.Instance.pressUseText.text = "E";
                            if (Input.GetKeyDown(KeyCode.E))
                            {
                                if (Inventory.FindItem("Key Part"))
                                {
                                    me.RPC("PlaceKey", RpcTarget.All, id, hit.transform.position.x, hit.transform.position.y, hit.transform.position.z, hit.transform.GetComponent<Door>().count);
                                    Inventory.DecreaseItemWithName("Key Part", 1);
                                    me.RPC("HotBar", RpcTarget.All, id, barCount, InventoryManager.Instance.hotBar[barCount - 1].name, InventoryManager.Instance.hotBar[barCount - 1].count);
                                }
                            }
                        }
                    }
                    else if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, 10, LayerMask.GetMask("Player")))
                    {
                        GameObject obj = hit.transform.gameObject;
                        if (werewolf)
                        {
                            if (obj.GetComponent<PlayerController>() && obj.GetComponent<PlayerController>().role.party == Classes.Party.Village && role.party == Classes.Party.Evil && aconiteTime <= 0)
                            {
                                NetworkingActor.Instance.leftClickPanel.SetActive(true);
                                if (Input.GetKeyDown(KeyCode.Mouse0))
                                {
                                    killing = true;
                                    SetAnimatorBool("isKill", true);
                                    SetAnimatorBool("isRunning", false);
                                    SetAnimatorBool("isWalking", false);
                                    SetAnimatorBool("isCrouch", false);
                                    SetAnimatorBool("isJump", false);
                                    rb.velocity = Vector3.zero + new Vector3(0, rb.velocity.y, 0);
                                    werewolfCoolDown -= 20f;

                                    Vector3 pos = transform.Find("KillPos").transform.position;

                                    obj.GetComponent<PhotonView>().RPC("Dead", RpcTarget.All, obj.GetComponent<PhotonView>().ViewID, transform.position.x, transform.position.z, pos.x, pos.y, pos.z, true);

                                    if (!obj.GetComponent<PlayerController>().aconite)
                                    {
                                        Invoke("KillFalse", 0.2f);
                                        Invoke("KillingFalse", 3f);

                                        Stats.GiveDamageToVillager(100f);
                                        Stats.KillVillager();
                                    }
                                    else
                                    {
                                        ExecuteWithDelay(0.5f, () => 
                                        { 
                                            killing = false; 
                                            KillFalse();

                                            me.RPC("Slow", RpcTarget.All, id, 5f);
                                            me.RPC("Aconite", RpcTarget.All, id, 5f);
                                        });
                                    }
                                }
                            }
                            else
                                NetworkingActor.Instance.leftClickPanel.SetActive(false);
                        }
                        else
                            NetworkingActor.Instance.leftClickPanel.SetActive(false);
                    }
                    else
                    {
                        NetworkingActor.Instance.pressUse.SetActive(false);
                        NetworkingActor.Instance.leftClickPanel.SetActive(false);
                        NetworkingActor.Instance.foodWarning.SetActive(false);
                        myObject = null;
                    }

                    #endregion
                }
            }
        }
        else
        {
            spineTarget.transform.localRotation = Quaternion.Euler(-anim.GetFloat("rotationY"), spineTarget.transform.localRotation.y, spineTarget.transform.localRotation.z);
            if (GameManager.Instance.isMe)
            {
                nameText.transform.LookAt(new Vector3(GameManager.Instance.isMe.transform.position.x,
                    nameText.transform.position.y, GameManager.Instance.isMe.transform.position.z));
            }

            if (!myPlayer)
            {
                PlayerController[] players = GameObject.FindObjectsOfType<PlayerController>();
                foreach (var player in players)
                {
                    if (player.me.IsMine)
                    {
                        myPlayer = player.gameObject;
                    }
                }
            }
            else
            {
                if (myPlayer.GetComponent<PlayerController>().role.party == Classes.Party.Evil)
                {
                    float dist = Vector3.Distance(myPlayer.transform.position, transform.position);

                    if (!ghost)
                    {
                        if (role.party == Classes.Party.Evil)
                        {
                            GetComponent<Outline>().OutlineColor = Color.red;
                            GetComponent<Outline>().enabled = true;
                        }
                        else
                        {
                            GetComponent<Outline>().OutlineColor = Color.white;
                            GetComponent<Outline>().enabled = dist <= 100 ? true : false;
                        }
                    }
                }
                else if(myPlayer.GetComponent<PlayerController>().role.party == Classes.Party.Neutral)
                {
                    GetComponent<Outline>().enabled = true;

                    if (role.party == Classes.Party.Evil)
                        GetComponent<Outline>().OutlineColor = Color.red;
                    else if (role.party == Classes.Party.Neutral)
                        GetComponent<Outline>().OutlineColor = Color.cyan;
                    else
                        GetComponent<Outline>().OutlineColor = Color.white;
                }
                else
                {
                    if(GetComponent<Outline>())
                    GetComponent<Outline>().enabled = false;
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (!isFinish && !dead && role.party == Classes.Party.Village)
        {
            Instantiate(Resources.Load<GameObject>("QuitPlayer"), transform.position, transform.rotation);
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Phase2"))
        {
            if (me.IsMine)
            {
                MissionsManager.Instance.CompleteMission("Go To Phase2");
                if (DayManager.Instance.isAnger)
                {
                    me.RPC("Dongu", RpcTarget.All, "night");
                }
            }
        }
        if (other.CompareTag("Escape"))
        {
            if (me.IsMine)
            {
                if (role.party == Classes.Party.Village)
                {
                    GameManager.Instance.inEscape = true;
                    GameManager.Instance.CounterStart();
                }
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Escape"))
        {
            if (me.IsMine)
            {
                GameManager.Instance.inEscape = false;
            }
        }
    }

    [PunRPC]
    public void HasAconite(int ID, bool active)
    {
        if(id == ID)
        {
            aconite = active;
        }
    }

    [PunRPC]
    public void SetVoiceImage(int ID, bool state)
    {
        if(id == ID)
        {
            if (!me.IsMine)
            {
                voiceImage.SetActive(state);
            }
        }
    }

    [PunRPC]
    public void SetCrate()
    {
        Inventory.setCrates = true;
    }

    [PunRPC]
    public void ZiplineActive(int ID, bool value, float time = 0f)
    {
        if(id == ID)
        {
            if (value)
            {
                Vector3[] path = new Vector3[myZipline.transform.GetChild(3).GetChild(0).GetChild(0).GetComponent<LineRenderer>().positionCount];
                myZipline.transform.GetChild(3).GetChild(0).GetChild(0).GetComponent<LineRenderer>().GetPositions(path);
                Vector3[] path3 = new Vector3[myZipline.transform.GetChild(3).GetChild(0).GetChild(0).GetComponent<LineRenderer>().positionCount - 1];

                for (int i = 0; i < path3.Length; i++)
                {
                    path3[i] = path[i + 1];
                }

                myLink = Instantiate(ziplineLink, transform.position, Quaternion.identity);
                myLink.SetActive(true);
                myLink.transform.DOPath(path3, time-0.2f).SetEase(Ease.Linear).OnUpdate(() => 
                {
                    myLink.transform.LookAt(myLinkConnectPoint.transform.position);
                    myLink.transform.localScale = new Vector3(myLink.transform.localScale.x, myLink.transform.localScale.y, Vector3.Distance(myLink.transform.position, myLinkConnectPoint.transform.position));
                });
            }
            else
            {
                Destroy(myLink.gameObject);
            }
        }
    }
    [PunRPC]
    public void PlaceKey(int ID, float x, float y, float z, int count)
    {
        if(id == ID)
        {
            GameObject keyPart = Instantiate(Resources.Load<GameObject>("Key Part"), transform.position, transform.rotation);
            keyPart.transform.DOMove(new Vector3(x, y + 10f, z), 5f).SetEase(Ease.Linear).OnComplete(() => 
            {
                for(int i = 0; i < GameManager.Instance.doors.Count; i++)
                {
                    GameManager.Instance.doors[i].PlacedKey();
                }
                Destroy(keyPart.gameObject);
            });
            keyPart.transform.DOLookAt(new Vector3(x, y + 10f, z), 1f);
        }
    }

    [PunRPC]
    public void QuitPlayer(bool ww)
    {
        if (me.IsMine)
        {
            GameManager.Instance.quitPlayer++;
        }
    }
    [PunRPC]
    public void Escaped(int ID)
    {
        if (me.IsMine)
        {
            GameManager.Instance.escapedPlayer++;
        }
        if (id == ID)
        {
            isFinish = true;
            if (!me.IsMine)
            {
                gameObject.SetActive(false);
            }
        }
    }
    [PunRPC]
    public void DeadPlayer(int ID)
    {
        if (id == ID)
        {
            GameManager.Instance.deadPlayer++;
        }
    }

    public bool isGround()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + new Vector3(0, 0.8f, 0), Vector3.down, out hit, 1.5f, ~LayerMask.GetMask("Hit") & ~LayerMask.GetMask("Player") & ~LayerMask.GetMask("DontJump") & ~LayerMask.GetMask("AI") & ~LayerMask.GetMask("ShadowOff")))
        {
            #if UNITY_EDITOR
            if (JumpTest)
            {
                Debug.LogError(hit.transform.name);
            }
            #endif
            return true;
        }
        else
        {
            return false;
        }
    }

    public IEnumerator WalkingAudio()
    {
        while(true)
        {
            if (!GetAnimatorBool("isWalking") && !GetAnimatorBool("isRunning"))
            {
                walkingAudio = false;
                break;
            }
            if (!isGround() || ragdoll || dead)
            {
                walkingAudio = false;
                break;
            }

            if (!werewolf)
            {
                myAudio.clip = NetworkingActor.Instance.walking[UnityEngine.Random.Range(0, NetworkingActor.Instance.walking.Length)];
                myAudio.Play();
            }
            else
            {
                myAudio.clip = NetworkingActor.Instance.walking[UnityEngine.Random.Range(0, NetworkingActor.Instance.walking.Length)];
                myAudio.Play();
            }

            if (GetAnimatorBool("isRunning"))
            {
                yield return new WaitForSeconds(0.35f);
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    [PunRPC]
    public void Slow(int ID, float time)
    {
        if (id == ID)
        {
            slowly = time;
        }
    }
    [PunRPC]
    public void Aconite(int ID, float time)
    {
        if (id == ID)
        {
            aconiteTime = time;
        }
    }

    [PunRPC]
    public void SendAudio(int ID, string clip)
    {
        if (id == ID)
        {
            switch (clip)
            {
                case "jumping":
                    myAudio.clip = NetworkingActor.Instance.jumping;
                    myAudio.Play();
                    break;
                case "changing":
                    myAudio.clip = NetworkingActor.Instance.wwChanging1;
                    myAudio.Play();
                    break;
                case "shotgun2Bullet":
                    gunAudio.clip = NetworkingActor.Instance.shotgun2BulletReloading;
                    gunAudio.Play();
                    break;
                case "shotgun1Bullet":
                    gunAudio.clip = NetworkingActor.Instance.shotgun1BulletReloading;
                    gunAudio.Play();
                    break;
                case "revolver":
                    gunAudio.clip = NetworkingActor.Instance.revolverReloading;
                    gunAudio.Play();
                    break;
                case "special":
                    gunAudio.clip = NetworkingActor.Instance.revolverReloading;
                    gunAudio.Play();
                    break;
                case "eat":
                    gunAudio.clip = NetworkingActor.Instance.eat;
                    gunAudio.Play();
                    break;
                case "wweat":
                    eatAudio.clip = NetworkingActor.Instance.wweat;
                    eatAudio.Play();
                    break;
                case "cough":
                    mouthAudio.clip = NetworkingActor.Instance.cough;
                    mouthAudio.Play();
                    break;
                case "takedamage":
                    eatAudio.clip = NetworkingActor.Instance.takeDamageAudio;
                    eatAudio.Play();
                    break;
                case "takeitem":
                    gunAudio.clip = NetworkingActor.Instance.takeItem;
                    gunAudio.Play();
                    break;
                case "zipline1":
                    myAudio.clip = NetworkingActor.Instance.zipline1;
                    myAudio.Play();
                    break;
                case "zipline2":
                    myAudio.clip = NetworkingActor.Instance.zipline2;
                    myAudio.Play();
                    ziplineAudio.clip = NetworkingActor.Instance.ziplineMain2;
                    ziplineAudio.Play();
                    break;
                case "zipline3":
                    myAudio.clip = NetworkingActor.Instance.zipline3;
                    myAudio.Play();
                    ziplineAudio.clip = NetworkingActor.Instance.ziplineMain3;
                    ziplineAudio.Play();
                    break;
                case "zipline4":
                    myAudio.clip = NetworkingActor.Instance.zipline4;
                    myAudio.Play();
                    ziplineAudio.clip = NetworkingActor.Instance.ziplineMain4;
                    ziplineAudio.Play();
                    break;
                case "zipline5":
                    myAudio.clip = NetworkingActor.Instance.zipline5;
                    myAudio.Play();
                    break;
                case "drop":
                    myAudio.clip = NetworkingActor.Instance.walking[0];
                    myAudio.Play();
                    break;
            }
        }
    }
    [PunRPC]
    public void StopAudio(int ID, string clip)
    {
        if (id == ID)
        {
            switch (clip)
            {
                case "cough":
                    mouthAudio.clip = null;
                    mouthAudio.Stop();
                    break;
                case "zipline":
                    myAudio.Stop();
                    ziplineAudio.Stop();
                    break;
            }
        }
    }

    [PunRPC]
    public void GasCloud(int phase)
    {
        switch(phase)
        {
            case 1:
                DayManager.Instance.StartCoroutine(DayManager.Instance.GasCloud(1));
                break;
            case 2:
                DayManager.Instance.StartCoroutine(DayManager.Instance.GasCloud(2));
                break;
        }
    }

    [PunRPC]
    public void DropItem(int ID, Vector3 location, string name, int count)
    {
        if (id == ID)
        {
            GameObject dropItem = Instantiate(Resources.Load<GameObject>("Objects"), location, Quaternion.identity);
            dropItem.GetComponent<PhotonView>().ViewID = GameManager.Instance.dropItemCount;
            GameManager.Instance.dropItemCount++;
            dropItem.GetComponent<Objects>().SetItem(name, 1);

            if (me.IsMine)
            {
                Inventory.DecreaseItem(barCount, 1);

                if (Inventory.FindItem_InHotBar("Aconite") != null && Inventory.FindItem_InHotBar("Aconite").name == "Aconite")
                {
                    me.RPC("HasAconite", RpcTarget.All, id, true);
                }
                else
                {
                    me.RPC("HasAconite", RpcTarget.All, id, false);
                }

                me.RPC("HotBar", RpcTarget.All, id, barCount, InventoryManager.Instance.hotBar[barCount - 1].name, InventoryManager.Instance.hotBar[barCount - 1].count);
            }
        }
    }

    [PunRPC]
    public void Reload(int ID)
    {
        if (id == ID)
        {
            float amount = 0;
            switch(item.bulletType)
            {
                case BulletType.mm9:
                    amount = revolverBulletAmount.y >= item.maxBulletInMagazine - revolverBulletAmount.x ? item.maxBulletInMagazine - revolverBulletAmount.x : revolverBulletAmount.y;
                    revolverBulletAmount.x += (int)amount;
                    revolverBulletAmount.y -= (int)amount;
                    break;
                case BulletType.Shotgun:
                    amount = shotgunBulletAmount.y >= item.maxBulletInMagazine - shotgunBulletAmount.x ? item.maxBulletInMagazine - shotgunBulletAmount.x : shotgunBulletAmount.y;
                    shotgunBulletAmount.x += (int)amount;
                    shotgunBulletAmount.y -= (int)amount;
                    break;
                case BulletType.Silver:
                    amount = silverBulletAmount.y >= item.maxBulletInMagazine - silverBulletAmount.x ? item.maxBulletInMagazine - silverBulletAmount.x : silverBulletAmount.y;
                    silverBulletAmount.x += (int)amount;
                    silverBulletAmount.y -= (int)amount;
                    break;
            }
        }
    }
    public void Changing()
    {
        if (!dead)
        {
            GameObject player = PhotonNetwork.Instantiate("Werewolf", transform.position, transform.rotation, 0, null);
            player.GetComponent<PlayerController>().werewolf = true;
            player.GetComponent<PlayerController>().werewolfCoolDown = 30f;
            player.GetComponent<PlayerController>().revolverBulletAmount = revolverBulletAmount;
            player.GetComponent<PlayerController>().shotgunBulletAmount = shotgunBulletAmount;
            Destroy(arm.transform.parent.gameObject);
            PhotonNetwork.Destroy(gameObject);
        }
    }

    public void Starting()
    {
        SetAnimatorBool("isPunch", true);

        rb = GetComponent<Rigidbody>();

        arm = transform.GetChild(1).GetChild(0).gameObject;
        body = transform.GetChild(0).gameObject;
        nameText = transform.Find("NameText").gameObject;
        voteText = transform.Find("VoteText").gameObject;
        voiceImage = transform.Find("VoiceImage").gameObject;
        cineCam = GameObject.FindObjectOfType<CinemachineVirtualCamera>();

        if (me.IsMine)
            recorder = GetComponent<PhotonVoiceView>().RecorderInUse;

        Cursor.lockState = CursorLockMode.Locked;

        GetComponent<PhotonView>().RPC("RagdollState", RpcTarget.All, id, false);

        voiceImage.SetActive(false);

        MyRoles();
    }

    public void DropAllItem()
    {
        string[] names = new string[Inventory.hotBarMeta.Count];
        int[] counts = new int[Inventory.hotBarMeta.Count];

        for (int i = 0; i < Inventory.hotBarMeta.Count; i++)
        {
            names[i] = Inventory.hotBarMeta[i].name;
            counts[i] = Inventory.hotBarMeta[i].count;
        }

        me.RPC("DropAllItemRPC", RpcTarget.All, id, names, counts);
    }

    [PunRPC]
    public void DropAllItemRPC(int ID, string[] names, int[] counts)
    {
        for (int i = 0; i < names.Length; i++)
        {
            for (int j = 0; j < counts[i]; j++)
            {
                float rndY = UnityEngine.Random.Range(0f, 360f);
                GameObject obj = Instantiate(Resources.Load<GameObject>("Objects"), transform.position + new Vector3(0, 1f, 0), Quaternion.Euler(0, rndY, 0));
                obj.GetComponent<PhotonView>().ViewID = GameManager.Instance.dropItemCount;
                GameManager.Instance.dropItemCount++;
                obj.SetActive(true);
                obj.GetComponent<Objects>().SetItem(names[i], 1);
            }
        }

        for (int i = 0; i < shotgunBulletAmount.y; i += 5)
        {
            float rndY = UnityEngine.Random.Range(0f, 360f);
            GameObject obj = Instantiate(Resources.Load<GameObject>("Objects"), transform.position + new Vector3(0, 1f, 0), Quaternion.Euler(0, rndY, 0));
            obj.GetComponent<PhotonView>().ViewID = GameManager.Instance.dropItemCount;
            GameManager.Instance.dropItemCount++;
            obj.SetActive(true);
            obj.GetComponent<Objects>().SetItem("ShotgunAmmo", 1);
        }
        for (int i = 0; i < revolverBulletAmount.y; i += 5)
        {
            float rndY = UnityEngine.Random.Range(0f, 360f);
            GameObject obj = Instantiate(Resources.Load<GameObject>("Objects"), transform.position + new Vector3(0, 1f, 0), Quaternion.Euler(0, rndY, 0));
            obj.GetComponent<PhotonView>().ViewID = GameManager.Instance.dropItemCount;
            GameManager.Instance.dropItemCount++;
            obj.SetActive(true);
            obj.GetComponent<Objects>().SetItem("9mmAmmo", 1);
        }
    }

    [PunRPC]
    public void Dead(int ID, float rotx, float rotz, float posx, float posy, float posz, bool ww)
    {
        if (id == ID)
        {
            if (ww)
            {
                GetComponent<Collider>().enabled = false;
                GetComponent<Rigidbody>().isKinematic = true;

                transform.DOMove(new Vector3(posx, posy, posz), 0.3f);
                transform.DOLookAt(new Vector3(rotx, transform.position.y, rotz), 0.3f);
                SetAnimatorBool("isDead", true);

                if (!aconite)
                {
                    Invoke("RagdollOpen", 3f);
                    ExecuteWithDelay(3f, () =>
                    {
                        Instantiate(NetworkingActor.Instance.deadParticle, spineBone.transform.position, Quaternion.identity);

                        if (me.IsMine && !werewolf)
                        {
                            DropAllItem();

                            Stats.Lose(GameManager.Instance.isMe.GetComponent<PlayerController>().surviveTime);

                            me.RPC("DeadPlayer", RpcTarget.All, id);
                        }
                    });
                }
                else
                {
                    ExecuteWithDelay(0.5f, () => 
                    {
                        SetAnimatorBool("isDead", false);
                        GetComponent<Collider>().enabled = true;
                        GetComponent<Rigidbody>().isKinematic = false;
                        dead = false;
                    });

                    if (me.IsMine)
                    {
                        Inventory.DecreaseItemWithName("Aconite", 1);

                        GetComponent<PhotonView>().RPC("HotBar", RpcTarget.All, id, barCount, InventoryManager.hotBarMeta[barCount].name, InventoryManager.hotBarMeta[barCount].count);

                        if (Inventory.FindItem_InHotBar("Aconite") != null && Inventory.FindItem_InHotBar("Aconite").name == "Aconite")
                        {
                            me.RPC("HasAconite", RpcTarget.All, id, true);
                        }
                        else
                        {
                            me.RPC("HasAconite", RpcTarget.All, id, false);
                        }
                    }
                }
            }
            else
            {
                RagdollOpen();

                Instantiate(NetworkingActor.Instance.deadParticle, spineBone.transform.position, Quaternion.identity);

                if (me.IsMine && werewolf)
                {
                    DropAllItem();

                    Invoke("RespawnWW", 5f);
                }
                if (me.IsMine && !werewolf)
                {
                    DropAllItem();

                    Stats.Lose(GameManager.Instance.isMe.GetComponent<PlayerController>().surviveTime);

                    if (role.party != Classes.Party.Evil)
                        me.RPC("DeadPlayer", RpcTarget.All, id);
                }
            }
            dead = true;
        }
    }

    public void RespawnGhost()
    {
        if (me.IsMine)
        {
            GameObject player = PhotonNetwork.Instantiate("DeadPlayer", transform.position, transform.rotation, 0, null);
            player.GetComponent<PlayerController>().werewolf = false;
            player.GetComponent<PlayerController>().ghost = true;
            Destroy(arm.transform.parent.gameObject);
            PhotonNetwork.Destroy(gameObject);

            Inventory.RemoveAllItem();

            Visibility.VisibiltyOn();
        }
    }
    public void RespawnWW()
    {
        if (me.IsMine)
        {
            if (!DayManager.Instance.phase2)
            {
                GameObject player = PhotonNetwork.Instantiate("Werewolf", GameManager.Instance.respawnLocations1[UnityEngine.Random.Range(0, GameManager.Instance.respawnLocations1.Length)].transform.position, transform.rotation, 0, null);
                player.GetComponent<PlayerController>().werewolf = false;
                Destroy(arm.transform.parent.gameObject);
                PhotonNetwork.Destroy(gameObject);
            }
            else
            {
                GameObject player = PhotonNetwork.Instantiate("Werewolf", GameManager.Instance.respawnLocations2[UnityEngine.Random.Range(0, GameManager.Instance.respawnLocations2.Length)].transform.position, transform.rotation, 0, null);
                player.GetComponent<PlayerController>().werewolf = false;
                Destroy(arm.transform.parent.gameObject);
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }

    public void KillFalse()
    {
        SetAnimatorBool("isKill", false);
    }
    public void KillingFalse()
    {
        killing = false;
        GetComponent<PhotonView>().RPC("KillingEffect", RpcTarget.All, id);
    }

    [PunRPC]
    public void KillingEffect(int ID)
    {
        if (id == ID)
        {
            Instantiate(bloodEffect, mouth.transform.position, mouth.transform.rotation).SetActive(true);
        }
    }
    public void RagdollOpen()
    {
        GetComponent<Collider>().enabled = true;
        GetComponent<Rigidbody>().isKinematic = false;

        myAudio.clip = NetworkingActor.Instance.dead;
        myAudio.Play();

        GetComponent<PhotonView>().RPC("RagdollState", RpcTarget.All, id, true);
    }

    [PunRPC]
    public void RagdollState(int ID, bool state)
    {
        if (ID == id)
        {
            ragdoll = state;

            RigBuilder builder = transform.GetComponentInChildren<RigBuilder>();
            if (builder)
                ExecuteWithDelay(0.1f, () => { builder.enabled = !state; });

            anim.enabled = !state;

            Rigidbody[] rigidbodies = body.transform.GetChild(0).GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody rigidbody in rigidbodies)
            {
                rigidbody.isKinematic = !state;
            }

            Collider[] colliders = body.transform.GetChild(0).GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
            {
                collider.isTrigger = !state;
            }

            if (me.IsMine)
            {
                cineCam.m_Follow = state ? head : head;
                cineCam.m_LookAt = state ? spineBone : null;

                Transform[] bodyObjs = body.transform.GetComponentsInChildren<Transform>();
                foreach (var obj in bodyObjs)
                {
                    if (obj.gameObject.layer == 7)
                    {
                        obj.gameObject.GetComponent<Renderer>().shadowCastingMode = state ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                    }
                }

                arm.SetActive(state ? false : true);
            }
        }
    }

    [PunRPC]
    public void Role()
    {
        if (GetComponent<MasterClient>())
        {
            MasterClient mc = GetComponent<MasterClient>();
            string[] parties = NetworkingManager.Instance.party;
            mc.playersRole = new Classes[parties.Length];

            for (int i = 0; i < parties.Length; i++)
            {
                for (int j = 0; j < ClassDataBase.Instance.roles.Count; j++)
                {
                    if (parties[i] == ClassDataBase.Instance.roles[j].party.ToString())
                    {
                        mc.playersRole[i] = ClassDataBase.Instance.roles[j];
                    }
                }
            }
        }
    }
    public void MyRoles()
    {
        for (int j = 0; j < NetworkingManager.Instance.nicks.Length; j++)
        {
            if (nick == NetworkingManager.Instance.nicks[j] && id.ToString().Substring(0, 1) ==
                NetworkingManager.Instance.ids[j].ToString().Substring(0, 1))
            {
                if (ghost)
                {
                    NetworkingManager.Instance.party[j] = "Neutral";
                }
                for (int i = 0; i < ClassDataBase.Instance.roles.Count; i++)
                {
                    if (ClassDataBase.Instance.roles[i].party.ToString() == NetworkingManager.Instance.party[j])
                    {
                        role.party = ClassDataBase.Instance.roles[i].party;
                        role.health = ClassDataBase.Instance.roles[i].health;
                        role.speed = ClassDataBase.Instance.roles[i].speed;

                        speed = baseSpeed;

                        if (role.party == Classes.Party.Evil)
                        {
                            if (transform.GetChild(0).gameObject.name == "Karakter_rigged")
                            {
                                health = 100;
                            }
                            else
                            {
                                werewolf = true;
                                health = role.health;
                                speed = baseSpeed * role.speed;
                            }
                        }
                        else
                        {
                            health = role.health;
                            speed = baseSpeed * role.speed;
                        }
                        return;
                    }
                }
            }
        }
    }

    [PunRPC]
    public void ChangeSkin(int ID, int count)
    {
        if(id == ID)
        {
            Renderer[] renderers = transform.GetComponentsInChildren<Renderer>();

            CustomizationManager custom = GameObject.FindObjectOfType<CustomizationManager>();

            foreach (Renderer renderer in renderers)
            {
                foreach (Material mat in renderer.materials)
                {
                    switch (mat.name)
                    {
                        case "player_skin_mat (Instance)":
                            mat.color = custom.customizationDataList[count].skinColor;
                            break;
                        case "Player_Hair (Instance)":
                            mat.color = custom.customizationDataList[count].hairColor;
                            break;
                        case "Player_Beard (Instance)":
                            mat.color = custom.customizationDataList[count].beardColor;
                            break;
                        case "player_tshirt_mat (Instance)":
                            mat.color = custom.customizationDataList[count].tshirtColor;
                            break;
                        case "player_pant_mat (Instance)":
                            mat.color = custom.customizationDataList[count].pantColor;
                            break;
                        case "player_shoe_mat (Instance)":
                            mat.color = custom.customizationDataList[count].shoeColor;
                            break;
                    }
                }
                switch (renderer.gameObject.name)
                {
                    case "Hair":
                        renderer.gameObject.SetActive(custom.customizationDataList[count].hair);
                        break;
                    case "T-shirt":
                        renderer.gameObject.SetActive(custom.customizationDataList[count].tshirt);
                        break;
                    case "T-shirt.001":
                        renderer.gameObject.SetActive(custom.customizationDataList[count].tshirt);
                        break;
                    case "T-shirt.002":
                        renderer.gameObject.SetActive(custom.customizationDataList[count].tshirt);
                        break;
                    case "Kemer":
                        renderer.gameObject.SetActive(custom.customizationDataList[count].belt);
                        break;
                    case "Sakal":
                        renderer.gameObject.SetActive(custom.customizationDataList[count].beard1);
                        break;
                    case "Sakal2":
                        renderer.gameObject.SetActive(custom.customizationDataList[count].beard2);
                        break;
                    case "Sakal3":
                        renderer.gameObject.SetActive(custom.customizationDataList[count].beard3);
                        break;
                    case "Mustache":
                        renderer.gameObject.SetActive(custom.customizationDataList[count].mustache1);
                        break;
                    case "Mustache2":
                        renderer.gameObject.SetActive(custom.customizationDataList[count].mustache2);
                        break;
                    case "Mustache3":
                        renderer.gameObject.SetActive(custom.customizationDataList[count].mustache3);
                        break;
                    case "SolKasPiercing":
                        renderer.gameObject.SetActive(custom.customizationDataList[count].leftEarning1);
                        break;
                    case "SolKulakPiercing":
                        renderer.gameObject.SetActive(custom.customizationDataList[count].leftEarning2);
                        break;
                    case "SolKüpe":
                        renderer.gameObject.SetActive(custom.customizationDataList[count].leftEarning3);
                        break;
                    case "SagKasPiercing":
                        renderer.gameObject.SetActive(custom.customizationDataList[count].rightEarning1);
                        break;
                    case "SagKulakPiercing":
                        renderer.gameObject.SetActive(custom.customizationDataList[count].rightEarning2);
                        break;
                    case "SagKüpe":
                        renderer.gameObject.SetActive(custom.customizationDataList[count].rightEarning3);
                        break;
                }
            }
        }
    }

    [PunRPC]
    public void SetMasterClient(int id)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].GetComponent<PhotonView>().ViewID == id)
            {
                if (!players[i].GetComponent<MasterClient>())
                    players[i].AddComponent<MasterClient>();
            }
        }
    }

    [PunRPC]
    public void HotBar(int ID, int bar, string itemName, int itemCount)
    {
        if (id == ID)
        {
            SetAnimatorBool("isIdle", false);

            barCount = bar;

            if (itemCount > 0)
            {
                if (lastItem)
                {
                    if (lastItem.GetComponent<Renderer>())
                    {
                        lastItem.GetComponent<Renderer>().enabled = false;
                        lastMainItem.GetComponent<Renderer>().enabled = false;
                    }
                    else
                    {
                        lastItem.transform.GetChild(0).gameObject.SetActive(false);
                        lastMainItem.transform.GetChild(0).gameObject.SetActive(false);
                    }
                }

                switch (itemName)
                {
                    case "Rifle":
                        lastItem = arm.transform.GetChild(0).GetComponentInChildren<Rifle>().gameObject;
                        lastMainItem = mainArm.transform.GetChild(0).GetComponentInChildren<Rifle>().gameObject;
                        bulletLoc = me.IsMine ? lastItem.transform.parent.Find("BulletLoc").gameObject : lastMainItem.transform.parent.Find("BulletLoc").gameObject;
                        break;
                    case "Revolver":
                        lastItem = arm.transform.GetChild(0).GetComponentInChildren<Magnum>().gameObject;
                        lastMainItem = mainArm.transform.GetChild(0).GetComponentInChildren<Magnum>().gameObject;
                        bulletLoc = me.IsMine ? lastItem.transform.parent.Find("BulletLoc").gameObject : lastMainItem.transform.parent.Find("BulletLoc").gameObject;
                        break;
                    case "KILLER OF PACK":
                        lastItem = arm.transform.GetChild(0).GetComponentInChildren<Special>().gameObject;
                        lastMainItem = mainArm.transform.GetChild(0).GetComponentInChildren<Special>().gameObject;
                        bulletLoc = me.IsMine ? lastItem.transform.parent.Find("BulletLoc").gameObject : lastMainItem.transform.parent.Find("BulletLoc").gameObject;
                        break;
                    case "Key Part":
                        lastItem = arm.transform.GetChild(0).GetComponentInChildren<KeyPart>().gameObject;
                        lastMainItem = mainArm.transform.GetChild(0).GetComponentInChildren<KeyPart>().gameObject;
                        break;
                    case "Dynamite":
                        lastItem = arm.transform.GetChild(0).GetComponentInChildren<Dynamite>().gameObject;
                        lastMainItem = mainArm.transform.GetChild(0).GetComponentInChildren<Dynamite>().gameObject;
                        break;
                    case "Syringe":
                        lastItem = arm.transform.GetChild(0).GetComponentInChildren<Syringe>().gameObject;
                        lastMainItem = mainArm.transform.GetChild(0).GetComponentInChildren<Syringe>().gameObject;
                        break;
                    case "Aconite":
                        lastItem = arm.transform.GetChild(0).GetComponentInChildren<Aconite>().gameObject;
                        lastMainItem = mainArm.transform.GetChild(0).GetComponentInChildren<Aconite>().gameObject;
                        break;
                    case "Hammer":
                        lastItem = arm.transform.GetChild(0).GetComponentInChildren<Hammer>().gameObject;
                        lastMainItem = mainArm.transform.GetChild(0).GetComponentInChildren<Hammer>().gameObject;
                        break;
                    case "Batt":
                        lastItem = arm.transform.GetChild(0).GetComponentInChildren<Baseball>().gameObject;
                        lastMainItem = mainArm.transform.GetChild(0).GetComponentInChildren<Baseball>().gameObject;
                        break;
                    case "Axe":
                        lastItem = arm.transform.GetChild(0).GetComponentInChildren<Axe>().gameObject;
                        lastMainItem = mainArm.transform.GetChild(0).GetComponentInChildren<Axe>().gameObject;
                        break;
                    case "Peach":
                        lastItem = arm.transform.GetChild(0).GetComponentInChildren<Peach>().gameObject;
                        lastMainItem = mainArm.transform.GetChild(0).GetComponentInChildren<Peach>().gameObject;
                        break;
                    case "Banana":
                        lastItem = arm.transform.GetChild(0).GetComponentInChildren<Banana>().gameObject;
                        lastMainItem = mainArm.transform.GetChild(0).GetComponentInChildren<Banana>().gameObject;
                        break;
                    case "Tomato":
                        lastItem = arm.transform.GetChild(0).GetComponentInChildren<Tomato>().gameObject;
                        lastMainItem = mainArm.transform.GetChild(0).GetComponentInChildren<Tomato>().gameObject;
                        break;
                }

                if (itemCount > 0)
                {
                    item = InventoryManager.FindItem_InDataBase(itemName);
                    item.count = itemCount;
                }

                SetAnimatorBool("isMelee", false);
                SetAnimatorBool("isRevolver", false);
                SetAnimatorBool("isRifle", false);
                SetAnimatorBool("isPunch", false);
                SetAnimatorBool("isFood", false);
                SetAnimatorBool("isSpecial", false);
                SetAnimatorBool($"is{item.animationIdle}", true);

                if (lastItem)
                {
                    if (lastItem.GetComponent<Renderer>())
                    {
                        lastItem.GetComponent<Renderer>().enabled = true;
                        lastMainItem.GetComponent<Renderer>().enabled = true;
                    }
                    else
                    {
                        lastItem.transform.GetChild(0).gameObject.SetActive(true);
                        lastMainItem.transform.GetChild(0).gameObject.SetActive(true);
                    }
                }
            }
            else
            {
                if (lastItem)
                {
                    if (lastItem.GetComponent<Renderer>())
                    {
                        lastItem.GetComponent<Renderer>().enabled = false;
                        lastMainItem.GetComponent<Renderer>().enabled = false;
                    }
                    else
                    {
                        lastItem.transform.GetChild(0).gameObject.SetActive(false);
                        lastMainItem.transform.GetChild(0).gameObject.SetActive(false);
                    }
                }
                SetAnimatorBool("isPunch", true);
                SetAnimatorBool("isMelee", false);
                SetAnimatorBool("isRevolver", false);
                SetAnimatorBool("isRifle", false);
                SetAnimatorBool("isFood", false);
                SetAnimatorBool("isSpecial", false);

                item = new Item();
            }
        }
    }

    [PunRPC]
    public void DamageEffect(int ID, Vector3 pos, Quaternion rot, bool melee)
    {
        if (id == ID)
        {
            GameObject effect = Instantiate(melee ? NetworkingActor.Instance.meleeBlood : NetworkingActor.Instance.gunBlood, pos, rot);
            effect.SetActive(true);
        }
    }
    [PunRPC]
    public void ArgentumEffect(int ID, Vector3 pos, Quaternion rot)
    {
        if (id == ID)
        {
            GameObject effect = Instantiate(NetworkingActor.Instance.argentumEffect, pos, rot);
            effect.SetActive(true);
        }
    }

    [PunRPC]
    public void SetBulletAmounts(int ID, float shotgunMin, float shotgunMax, float revolverMin, float revolverMax, float silverMin, float silverMax)
    {
        if (id == ID)
        {
            revolverBulletAmount.x = (int)revolverMin;
            revolverBulletAmount.y = (int)revolverMax;
            shotgunBulletAmount.x = (int)shotgunMin;
            shotgunBulletAmount.y = (int)shotgunMax;
            silverBulletAmount.x = (int)silverMin;
            silverBulletAmount.y = (int)silverMax;

            anim.SetInteger("ShotgunBullet", (int)shotgunBulletAmount.x);
            anim.SetInteger("RevolverBullet", (int)revolverBulletAmount.x);
        }
    }

    [PunRPC]
    public void Attack(int ID, Vector3 pos, Vector3 way)
    {
        if (id == ID)
        {
            if (GetAnimatorBool("isReloading"))
                return;
            if (item.bulletType == BulletType.mm9 && revolverBulletAmount.x <= 0 || item.bulletType == BulletType.Shotgun && shotgunBulletAmount.x <= 0 || item.bulletType == BulletType.Silver && silverBulletAmount.x <= 0)
            {
                gunAudio.clip = NetworkingActor.Instance.noBullet;
                gunAudio.Play();
                if (me.IsMine)
                {
                    NetworkingActor.Instance.bulletAmount.color = Color.red;
                    ExecuteWithDelay(0.1f, () =>
                    {
                        NetworkingActor.Instance.bulletAmount.color = Color.black;
                    });
                }
                return;
            }

            SetAnimatorBool("isAttack", true);

            if (!GetAnimatorBool("isMelee") && !GetAnimatorBool("isPunch"))
            {
                switch (item.name)
                {
                    case "Rifle":
                        ExecuteWithDelay(AnimationLenght("Attack_Rifle"), () => { SetAnimatorBool("isAttack", false); });
                        gunAudio.clip = NetworkingActor.Instance.shotgunShoot;
                        gunAudio.Play();
                        bulletLoc = me.IsMine ? lastItem.transform.parent.Find("BulletLoc").gameObject : lastMainItem.transform.parent.Find("BulletLoc").gameObject;
                        break;
                    case "Revolver":
                        ExecuteWithDelay(AnimationLenght("Attack_Revolver") / 3f, () => { SetAnimatorBool("isAttack", false); });
                        gunAudio.clip = NetworkingActor.Instance.revolverShoot;
                        gunAudio.Play();
                        bulletLoc = me.IsMine ? lastItem.transform.parent.Find("BulletLoc").gameObject : lastMainItem.transform.parent.Find("BulletLoc").gameObject;
                        break;
                    case "KILLER OF PACK":
                        ExecuteWithDelay(AnimationLenght("Attack_Special"), () => { SetAnimatorBool("isAttack", false); });
                        gunAudio.clip = NetworkingActor.Instance.specialShoot;
                        gunAudio.Play();
                        bulletLoc = me.IsMine ? lastItem.transform.parent.Find("BulletLoc").gameObject : lastMainItem.transform.parent.Find("BulletLoc").gameObject;
                        break;
                }

                if (bulletLoc)
                {
                    GameObject newParticle = Instantiate(explosionEffect, bulletLoc.transform.position, Quaternion.identity);
                    newParticle.transform.LookAt(way);
                    SecondaryHit(UnityEngine.Random.Range(item.damage.x, item.damage.y), pos, way);
                }
            }
            if (GetAnimatorBool("isMelee"))
            {
                ExecuteWithDelay(0.2f, () =>
                {
                    gunAudio.clip = NetworkingActor.Instance.meleeShoot;
                    gunAudio.Play();
                });
                ExecuteWithDelay(AnimationLenght("Attack") / 1.6f, () => { SetAnimatorBool("isAttack", false); });
                ExecuteWithDelay(0.4f, () =>
                {
                    RaycastHit gunHit;
                    if (Physics.Raycast(cineCam.transform.position, cineCam.transform.forward, out gunHit, 3))
                    {
                        if (gunHit.transform.GetComponent<ArgentumOre>())
                        {
                            float rndDmg = 0;
                            rndDmg = UnityEngine.Random.Range(5f, 10f);

                            gunHit.transform.GetComponent<PhotonView>().RPC("Damage", RpcTarget.All, gunHit.transform.GetComponent<PhotonView>().ViewID, rndDmg);
                            me.RPC("ArgentumEffect", RpcTarget.All, id, gunHit.point, transform.rotation);

                            if (gunHit.transform.GetComponent<ArgentumOre>().health <= 0)
                            {
                                AchievementManager.Instance.SetAchievement("Miner");
                            }

                            if (me.IsMine)
                            {
                                GameManager.Instance.CrossChanginng();
                            }
                        }
                    }
                });
            }
            if (GetAnimatorBool("isPunch"))
            {
                ExecuteWithDelay(0.2f, () =>
                {
                    gunAudio.clip = NetworkingActor.Instance.meleeShoot;
                    gunAudio.Play();
                });
                ExecuteWithDelay(AnimationLenght("Punch"), () => { SetAnimatorBool("isAttack", false); });
                if (GetComponent<PhotonView>().IsMine)
                {
                    ExecuteWithDelay(0.6f, () =>
                    {
                        RaycastHit gunHit;
                        if (Physics.Raycast(cineCam.transform.position, cineCam.transform.forward, out gunHit, 3))
                        {
                            if (gunHit.transform.GetComponent<PlayerController>() && !gunHit.transform.GetComponent<PlayerController>().ghost)
                            {
                                float rndDmg = 0;
                                if (item.count > 0)
                                    rndDmg = UnityEngine.Random.Range(item.damage.x, item.damage.y);
                                else
                                    rndDmg = UnityEngine.Random.Range(5f, 10f);

                                gunHit.transform.GetComponent<PlayerController>().me.RPC("Hit", RpcTarget.All, gunHit.transform.GetComponent<PlayerController>().id, rndDmg);
                                me.RPC("DamageEffect", RpcTarget.All, id, gunHit.point, transform.rotation, true);

                                if (me.IsMine)
                                {
                                    GameManager.Instance.CrossChanginng();
                                    if (gunHit.transform.GetComponent<PlayerController>().role.party == Classes.Party.Evil)
                                    {
                                        Stats.GiveDamageToWerewolf(rndDmg);
                                        if (gunHit.transform.GetComponent<PlayerController>().health <= 0)
                                        {
                                            Stats.KillWerewolf();

                                            AchievementManager.Instance.SetAchievement("Silver Shot");
                                        }
                                    }
                                    else
                                    {
                                        Stats.GiveDamageToVillager(rndDmg);
                                        if (gunHit.transform.GetComponent<PlayerController>().health <= 0)
                                        {
                                            Stats.KillVillager();
                                        }
                                    }
                                    if (gunHit.transform.GetComponent<PlayerController>().role.party == Classes.Party.Village)
                                    {
                                        if (gunHit.transform.GetComponent<PlayerController>().health <= 0)
                                        {
                                            AchievementManager.Instance.SetAchievement("Killer");
                                        }
                                    }
                                }
                            }
                            if (gunHit.transform.GetComponent<ArgentumOre>())
                            {
                                float rndDmg = 0;
                                rndDmg = UnityEngine.Random.Range(5f, 10f);

                                gunHit.transform.GetComponent<PhotonView>().RPC("Damage", RpcTarget.All, gunHit.transform.GetComponent<PhotonView>().ViewID, rndDmg);
                                me.RPC("ArgentumEffect", RpcTarget.All, id, gunHit.point, transform.rotation);

                                if (gunHit.transform.GetComponent<ArgentumOre>().health <= 0)
                                {
                                    AchievementManager.Instance.SetAchievement("Miner");
                                }

                                if (me.IsMine)
                                {
                                    GameManager.Instance.CrossChanginng();
                                }
                            }
                            if (gunHit.transform.GetComponent<HitBodyAI>())
                            {
                                float rndDmg = 0;
                                rndDmg = UnityEngine.Random.Range(5f, 10f);

                                gunHit.transform.GetComponent<HitBodyAI>().controller.GetComponent<PhotonView>().RPC("Hit", RpcTarget.All, gunHit.transform.GetComponent<HitBodyAI>().controller.GetComponent<PhotonView>().ViewID, rndDmg, id);
                                me.RPC("DamageEffect", RpcTarget.All, id, gunHit.point, transform.rotation, true);

                                if (me.IsMine)
                                {
                                    GameManager.Instance.CrossChanginng();

                                    if (gunHit.transform.GetComponent<PlayerController>().health <= 0)
                                    {
                                        AchievementManager.Instance.SetAchievement("Monster Hunter");
                                    }
                                }
                            }
                        }
                    });
                }
            }
            if (item.coolDown > 0)
                coolDown = item.coolDown;
            else
            {
                coolDown = AnimationLenght("Punch");
            }

            me.RPC("SetBulletAmounts", RpcTarget.All, id, shotgunBulletAmount.x, shotgunBulletAmount.y, revolverBulletAmount.x, revolverBulletAmount.y, silverBulletAmount.x, silverBulletAmount.y);
        }
    }
    public float AnimationLenght(string name)
    {
        AnimationClip[] clips = arm.GetComponent<Animator>().runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            if (clip.name == name)
            {
                return clip.length;
            }
        }
        return 1f;
    }
    [PunRPC]
    public void Hit(int ID, float damage)
    {
        if (id == ID)
        {
            health -= damage;
            if (me.IsMine)
            {
                NetworkingActor.Instance.takeDamageImage.GetComponent<Animator>().Play("takedamage");
                me.RPC("SendAudio", RpcTarget.All, id, "takedamage");

                AchievementManager.Instance.SetAchievement("Iron Man", damage);
            }
        }
    }
    public void SecondaryHit(float damage, Vector3 pos, Vector3 way)
    {
        SetAnimatorBool("isIdle", false);
        GameObject newBullet = null;
        if (item.bulletType == BulletType.mm9)
        {
            newBullet = Instantiate(Resources.Load<GameObject>("Bullet"), pos, Quaternion.identity);
            newBullet.transform.LookAt(way);
            newBullet.GetComponent<Bullets>().owner = GetComponent<PhotonView>();
            newBullet.GetComponent<Bullets>().damage = damage;
        }
        if (item.bulletType == BulletType.Shotgun)
        {
            newBullet = Instantiate(Resources.Load<GameObject>("ShotgunBullet"), pos, Quaternion.identity);
            newBullet.transform.LookAt(way);
            Bullets[] bullets = newBullet.transform.GetComponentsInChildren<Bullets>();
            foreach(Bullets bullet in bullets)
            {
                bullet.owner = GetComponent<PhotonView>();
                bullet.damage = damage / bullets.Length;

                Vector3 lookingWay = way
                    + newBullet.transform.right * UnityEngine.Random.Range(-5f, 5f)
                    + newBullet.transform.up * UnityEngine.Random.Range(-5f, 5f);

                bullet.transform.LookAt(lookingWay);
            }
        }
        if (item.bulletType == BulletType.Silver)
        {
            newBullet = Instantiate(Resources.Load<GameObject>("Bullet"), pos, Quaternion.identity);
            newBullet.transform.LookAt(way);
            newBullet.GetComponent<Bullets>().owner = GetComponent<PhotonView>();
            newBullet.GetComponent<Bullets>().damage = damage;
            newBullet.GetComponent<Bullets>().silver = true;
        }

        BulletState(item.bulletType, 1, "-");
    }

    public void BulletState(BulletType type, int count, string process)
    {
        switch (type)
        {
            case BulletType.mm9:
                if (process == "+")
                    revolverBulletAmount.x += count;
                else
                    revolverBulletAmount.x -= count;
                break;
            case BulletType.Shotgun:
                if (process == "+")
                    shotgunBulletAmount.x += count;
                else
                    shotgunBulletAmount.x -= count;
                break;
            case BulletType.Silver:
                if (process == "+")
                    silverBulletAmount.x += count;
                else
                    silverBulletAmount.x -= count;
                break;
        }
    }

    public void SetAnimatorBool(string name, bool state)
    {
        anim.SetBool(name, state);
    }

    public bool GetAnimatorBool(string name)
    {
        return anim.GetBool(name);
    }

    public void AnimatorPlay(string name)
    {
        arm.GetComponent<Animator>().Play(name);
        anim.Play(name);
    }
    
    [PunRPC]
    public void Dongu(string state)
    {
        switch (state)
        {
            case "night":
                DayManager.Instance.Night();
                break;
            case "anger":
                DayManager.Instance.Anger();
                break;
        }
    }

    [PunRPC]
    public void TakeWWCount(int wwcount)
    {
        RoomSettings.instance.wwCount = wwcount;
    }

    private void ExecuteWithDelay(float seconds, Action action)
    {
        StartCoroutine(ExecuteAction(seconds, action));
    }
    private IEnumerator ExecuteAction(float seconds, Action action)
    {
        yield return new WaitForSecondsRealtime(seconds);

        action();
    }
}
