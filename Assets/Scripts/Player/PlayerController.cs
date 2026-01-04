using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;

public class PlayerController : MonoBehaviour
{
    [Header("事件监听")]
    public SceneLoadEventSO sceneLoadEvent;
    public VoidEventSO afterSceneLoadedEvent;
    public VoidEventSO loadDataEvent;
    public VoidEventSO backToMenuEvent;
    public VoidEventSO winEvent;

    public PlayerInputControl inputControl;
    public Vector2 inputDirection;
    private Rigidbody2D rb;
    private PhysicsCheck physicsCheck;
    private CapsuleCollider2D coll;
    private PlayerAnimations playerAnimations;
    private Character character;

    [Header("基本参数")]
    public float speed;

    public float runSpeed;
    public float walkSpeed;

    public float jumpForce;
    public float wallJumpForce;
    public float slideDistance;
    public float slideSpeed;
    public int slidePowerCost;

    private Vector2 originalOffset;
    private Vector2 originalSize;

    public float hurtForce;

    [Header("动作判定参数")]
    [Tooltip("长按阈值（秒）：超过此时间触发跑步，否则触发滑铲")]
    public float holdThreshold = 0.3f;
    [Tooltip("滑铲持续时间（秒）")] public float slideDuration = 0.5f;

    [Header("物理材质")]
    public PhysicsMaterial2D normal;
    public PhysicsMaterial2D wall;

    [Header("状态")]
    public bool isCrouch;
    public bool isAttack;
    public bool isHurt;
    public bool isDead;
    public bool wallJump;
    public bool isSlide;         //是否正在滑铲
    public bool isRunning;       // 是否正在跑步
    
    private bool isShiftPressed; // Shift键是否按下
    private float shiftPressTime; // Shift按下的起始时间
    public bool isDefence;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        physicsCheck = GetComponent<PhysicsCheck>();
        inputControl = new PlayerInputControl();
        coll = GetComponent<CapsuleCollider2D>();
        playerAnimations = GetComponent<PlayerAnimations>();
        character = GetComponent<Character>();
        originalOffset = coll.offset;
        originalSize = coll.size;

        inputControl.GamePlay.Jump.started += Jump;
        //speed初始赋值
        speed = walkSpeed;

        inputControl.GamePlay.Attack.started += PlayerAttack;

        inputControl.GamePlay.Defence.started += PlayerDefence;
        inputControl.GamePlay.Defence.canceled += PlayerDefenceExit;

        //滑铲
        //inputControl.GamePlay.Slide.started += Slide;


        inputControl.GamePlay.Slide.performed += OnShiftPressed;
        inputControl.GamePlay.Slide.canceled += OnShiftReleased;
        inputControl.Enable();
    }

    private void OnEnable()
    {
        sceneLoadEvent.LoadRequestEvent += OnLoadEvent;
        afterSceneLoadedEvent.OnEventRaised += OnAfterSceneLoadEvent;
        loadDataEvent.OnEventRaised += OnLoadDataEvent;
        backToMenuEvent.OnEventRaised += OnLoadDataEvent;
        winEvent.OnEventRaised += OnWinEvent;
    }


    private void OnDisable()
    {
        inputControl.Disable();
        sceneLoadEvent.LoadRequestEvent -= OnLoadEvent;
        afterSceneLoadedEvent.OnEventRaised -= OnAfterSceneLoadEvent;
        loadDataEvent.OnEventRaised -= OnLoadDataEvent;
        backToMenuEvent.OnEventRaised -= OnLoadDataEvent;
        winEvent.OnEventRaised -= OnWinEvent;
    }



    private void Update()
    {
        inputDirection = inputControl.GamePlay.Move.ReadValue<Vector2>();
        HandleShiftHoldLogic();  // 处理长按跑步逻辑
        CheckState();
    }

    private void FixedUpdate()
    {
        if(!isHurt && !isAttack &&!isDefence)
            Move();
    }

    private void OnLoadEvent(GameSceneSO arg0, Vector3 arg1, bool arg2)
    {
        inputControl.GamePlay.Disable();
    }

    private void OnLoadDataEvent()
    {
        isDead = false;
    }
    private void OnAfterSceneLoadEvent()
    {
        inputControl.GamePlay.Enable();
    }
    public void Move()
    {
        if(!isCrouch && !wallJump)
            rb.velocity = new Vector2(inputDirection.x * speed * Time.deltaTime, rb.velocity.y);

        int faceDir = (int)transform.localScale.x;

        //通过刚体速度判断面朝方向
        if (rb.velocity.x > 0)
            faceDir = 1;
        if (rb.velocity.x < 0)
            faceDir = -1;

        transform.localScale = new Vector3(faceDir, 1, 1);

        isCrouch = inputDirection.y < -0.5f && physicsCheck.isGround;
        if (isCrouch)
        {
            coll.offset = new Vector2(-0.05f, 0.85f);
            coll.size = new Vector2(0.7f, 1.7f);
        }
        else
        {
            coll.size = originalSize;
            coll.offset = originalOffset;
        }
               
    }
    private void Jump(InputAction.CallbackContext context)
    {
        if (physicsCheck.isGround)
        {
            rb.AddForce(transform.up * jumpForce, ForceMode2D.Impulse);
            GetComponent<AudioDefination>()?.PlayAudioClip();
            //打断滑铲协程
            isSlide = false;
            StopAllCoroutines();
        }
        else if (physicsCheck.onWall)
        {
            rb.AddForce(new Vector2(-inputDirection.x, 2.5f) * wallJumpForce, ForceMode2D.Impulse);
            GetComponent<AudioDefination>()?.PlayAudioClip();
            wallJump = true;
        }
    }
    private void PlayerAttack(InputAction.CallbackContext context)
    {
        if (physicsCheck.isGround)
        {
            playerAnimations.PlayerAttack();
            isAttack = true;
        }
    }

    private void PlayerDefence(InputAction.CallbackContext context)
    {
        if (physicsCheck.isGround)
        {
            isDefence = true;

            character.Defence = true;
        }
    }


    private void PlayerDefenceExit(InputAction.CallbackContext context)
    {
        isDefence = false;
        character.Defence = false;
    }
    private void OnShiftPressed(InputAction.CallbackContext context)
    {
        isShiftPressed = true;
        shiftPressTime = Time.time; // 记录按下时间
        isSlide = false; // 重置滑铲状态（防止重复触发）
    }
    private void OnShiftReleased(InputAction.CallbackContext context)
    {
        isShiftPressed = false;

        // 判断按下时长：短按触发滑铲，长按结束跑步
        float pressDuration = Time.time - shiftPressTime;
        if (pressDuration < holdThreshold && !isRunning && character.currentPower > 0)
        {
            Slide(); // 短按：触发滑铲
        }
        else if (isRunning)
        {
            speed = walkSpeed; // 长按释放：结束跑步
            isRunning = false;
            character.Running = false;
        }
    }

    // 处理长按逻辑：超过阈值且能量足够时进入跑步状态
    private void HandleShiftHoldLogic()
    {
        if (isShiftPressed && !isSlide && Mathf.Abs(rb.velocity.x) > 0.1)
        {
            float pressDuration = Time.time - shiftPressTime;
            if (pressDuration >= holdThreshold && character.currentPower > 1 && !isRunning)
            {
                isRunning = true;
                speed = runSpeed; // 进入跑步状态
                character.Running = true;
            }
        }
    }

    private void Slide()
    {
        if (!isSlide && physicsCheck.isGround && character.currentPower >= slidePowerCost)
        {
            isSlide = true;

            var targetPos = new Vector3(transform.position.x + slideDistance * transform.localScale.x, transform.position.y);

            gameObject.layer = LayerMask.NameToLayer("Enemy");
            StartCoroutine(TriggerSlide(targetPos));

            character.OnSlide(slidePowerCost);
        }
    }
    private IEnumerator TriggerSlide(Vector3 target)
    {
        do
        {
            yield return null;
            if (!physicsCheck.isGround)
                break;

            //滑铲过程中撞墙
            if (physicsCheck.touchLeftWall && transform.localScale.x < 0f || physicsCheck.touchRightWall && transform.localScale.x > 0f)
            {
                isSlide = false;
                break;
            }

            rb.MovePosition(new Vector2(transform.position.x + transform.localScale.x * slideSpeed, transform.position.y));
        } while (MathF.Abs(target.x - transform.position.x) > 0.1f);

        isSlide = false;
        gameObject.layer = LayerMask.NameToLayer("Player");
    }

    public void GetHurt(Transform attacker)
    {
        isHurt = true;
        rb.velocity = Vector2.zero;
        Vector2 dir = new Vector2((transform.position.x - attacker.position.x), 0).normalized;

        rb.AddForce(dir * hurtForce, ForceMode2D.Impulse);
    }

    public void PlayerDead()
    {
        isDead = true;
        inputControl.GamePlay.Disable();
    }

    public void OnWinEvent()
    {
        inputControl.GamePlay.Disable();
    }


    private void CheckState()
    {
        coll.sharedMaterial = physicsCheck.isGround ? normal : wall;

        if (physicsCheck.onWall)
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y / 2f);
        else
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y);

        if (wallJump && rb.velocity.y < 0f)
        {
            wallJump = false;
        }

        if (character.currentPower <= 1)
        {
            isDefence = false;
            speed = walkSpeed; // 退出跑步状态
            isRunning = false;
        }

        if (Mathf.Abs(rb.velocity.x) < 0.1)
        {
            isRunning = false;
            character.Running = false;
        }

        if (isDead || isSlide)
            gameObject.layer = LayerMask.NameToLayer("Enemy");
        else
            gameObject.layer = LayerMask.NameToLayer("Player");
    }
}
