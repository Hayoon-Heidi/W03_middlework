﻿using UnityEngine;
using System.Collections;
using System;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Processors;
using UnityEngine.Windows;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;
//using UnityGLTF.Extensions;

/*
 * W03_8팀_구본재문준호유승훈김하윤
 * Player Controller Script
 */


public class Controller : MonoBehaviour
{
    #region INPUT SYSTEM


    InputSystem action; // Input system
    InputAction groundMoveAction; // 땅 위에 있을 때 받는 인풋 값.
    InputAction airMoveAction; // 점프 후 공중에서 떠있을 때 받는 인풋 값 
    InputAction jumpAction; // 점프를 시키는 인풋 값
    InputAction reviveAction; // 다시 살리는 인풋 값
    InputAction restartAction; // 재시작을 위한 인풋값

    #endregion

    #region VARIABLES 

    [Header("Tilting Variables")]
    [SerializeField] float tilt; // 기울기값 저장 변수
    [Space(5)]

    [Header("Vector3 Variables")]
    [SerializeField] Vector3 velocity; // 현재속도저장 벡터
    [SerializeField] Vector3 localVel; // 로컬 좌표계 속도 저장 벡터
    [SerializeField] Vector3 curNormal = Vector3.up;
    [SerializeField] Vector3 moveDirection; // 이동 방향
    [SerializeField] Vector3 normalGround, posGround;
    [SerializeField] Vector3 tiltingAngle;
    [SerializeField] Vector2 moveInput;
    [SerializeField] Vector3 CurTransform; // the place of player dead

    [Header("Strength")]
    [SerializeField] float turnStrength = 0.1f; // 회전 강도
    [SerializeField] float jumpForce = 10f; // 점프 힘
    [SerializeField] float VelForZeroGravity = 5; // 중력을 0으로 만들기 위한 속도 값
    [SerializeField] float reduceGravity = -5f;
    [SerializeField] float originalGravity = -9.81f;
    [SerializeField] float magnitude;
    [SerializeField] float minMagnitude = 3.0f; // 속도가 일정 수치 이하인 경우 드래그 값을 0으로 주는 기준
    [SerializeField] float dragStrenght = 200f; // 속도가 일정 수치 이상인 경우 드래그 값을 해당 값으로 나눔
    [SerializeField] float rotationSpeed = 300f; // 회전 속도
    [SerializeField] float changeGravity = -15f;
    [Space(5)]

    [Header("Distance")]
    [SerializeField] float groundCheckDistance = 0.2f; // 레이캐스트를 위한 땅과 보드 사이의 거리 확인 변수
    [SerializeField] float distGround, distGroundL, distGroundR;// 보드가 바닥에서 얼마나 떨어져있는지를 확인하기 위한 변수
    [SerializeField] float boardDeltaY; // 보드의 회전 각도
    [Space(5)]

    [Header("RigidBody & Transform for Ray")]
    [SerializeField] Rigidbody rg; // 플레이어의 리지드바디
    [SerializeField] Transform R, L; // 플레이어 양 옆에 붙어있는 오브젝트의 트랜스폼
    [SerializeField] RaycastHit hit; // 플레이어 양 옆에 붙어있는 오브젝트로 확인
    [Space(5)]

    [Header("Layer Mask")]
    [SerializeField] LayerMask groundLayer; // 땅 체크를 위한 레이어 확인
    [SerializeField] GameObject ground;
    [SerializeField] private bool isGrounded; // 플레이어가 땅 위에 있는지에 대한 여부 판별
    [SerializeField] private bool isJumping;
    [SerializeField] private bool jumpRequested; // 시스템 인풋으로 점프 요청 여부 판별
    [SerializeField] private bool pressR;
    [SerializeField] private bool isDead;

    [SerializeField] TMP_Text infoText;


    #endregion

    #region private void AWAKE 
    private void Awake()
    {
        action = new InputSystem();
        groundMoveAction = action.Player.GroundMove;
        airMoveAction = action.Player.AirMove;
        jumpAction = action.Player.Jump;
        reviveAction = action.Player.Revive;
        restartAction = action.Player.Restart;
        rg = GetComponent<Rigidbody>();

        isDead = false;
    }

    #endregion

    #region Enable & Disable

    private void OnEnable()
    {
        // 이벤트 구독
        jumpAction.performed += OnJump;
        action.Enable();
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        jumpAction.performed -= OnJump;
        action.Disable();
    }

    #endregion

    #region void UPDATE

    void Update()
    {
        isGrounded = Physics.Raycast(transform.position, -Vector3.up, groundCheckDistance, groundLayer);

        BasicMoving(moveInput);
        CheckGroundRay();

        CollisionTriggerDeltaTime += Time.deltaTime;
    }

    #endregion

    #region void FIXEDUPDATE

    void FixedUpdate()
    {
        MoveForward();
        AddDrag();
        TiltAndJump();
        AddDrag();

        rg.angularVelocity = Vector3.zero;
        
    }

    #endregion

    #region BASIC MOVING (FROM UPDATE)
    void BasicMoving(Vector2 moveInput)
    {
        if (isGrounded && isJumping && !isDead) { 
            isJumping = false;
            Physics.gravity = new Vector3(0, originalGravity, 0);
        }
           
        if (isGrounded && !isDead)
        {
            groundMoveAction.Enable();
            airMoveAction.Disable();
            moveInput = groundMoveAction.ReadValue<Vector2>();
            tilt = moveInput.x;

            moveDirection = transform.forward * moveInput.y;

            if (moveInput.y > 0)
            {
                Vector3 force = (Vector3.down * rg.mass);
                rg.AddForce(force);
            }
            else if (moveInput.y < 0)
            {
                rg.velocity = Vector3.Lerp(rg.velocity, Vector3.zero, Time.deltaTime);

                if (rg.velocity.magnitude < VelForZeroGravity)
                {
                    Physics.gravity = Vector3.zero;
                }
            }
            else if (moveInput.y == 0)
            {
                Physics.gravity = new Vector3(0, -9.8f, 0);
            }
        }
        else if (!isGrounded && !isDead) 
        {
            airMoveAction.Enable();
            groundMoveAction.Disable();
            moveInput = airMoveAction.ReadValue<Vector2>();
            tilt = moveInput.x;
        }
    }
    #endregion

    #region CHECK GROUND USING RAY (FROM UPDATE)

    void CheckGroundRay()
    {
        if (Physics.Raycast(L.position, -curNormal, out hit))
        {
            posGround = hit.point; // 충돌 지점
            distGroundL = hit.distance; // 거리
            normalGround = hit.normal; // hit.point 법선, 기울어진 표면의 항상 수진인 법선 
        }
        if (Physics.Raycast(R.position, -curNormal, out hit))
        {
            posGround = (posGround + hit.point) / 2f;
            if (hit.point.y > posGround.y)
                posGround.y = hit.point.y;
            distGroundR = hit.distance;
        }
        distGround = (distGroundL + distGroundR) / 2f;
    }

    #endregion

    #region MOVE FORWARD (FROM FIXED UPDATE)

    void MoveForward()
    {
        boardDeltaY = 0; // y축 변화값
        velocity = rg.velocity;
        localVel = transform.InverseTransformDirection(velocity);
    }

    #endregion

    #region ADD DRAG (FROM FIXED UPDATE)

    void AddDrag()
    {
        //Simulate friction by increasing the drag depending of the speed
        magnitude = velocity.magnitude;
        if (magnitude < minMagnitude)
            rg.drag = 0;
        else
            rg.drag = magnitude / dragStrenght;
    }

    #endregion

    #region TILT AND JUMP (FROM FIXED UPDATE)

    void TiltAndJump()
    {
        if (isGrounded && !isDead)
        {
            //On the ground/snow
            boardDeltaY += (float)(tilt * (1 + velocity.magnitude / 10f));
            // tiltingAngle = transform.eulerAngles;
            // tiltingAngle.y += boardDeltaY;
            // transform.eulerAngles = tiltingAngle;
            Quaternion targetRotation = Quaternion.Euler(new Vector3(transform.eulerAngles.x, transform.eulerAngles.y + boardDeltaY, transform.eulerAngles.z));
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 20);
            // float moveInput = Mathf.Clamp(Vector3.Dot(localVel, moveDirection), -maxspeed, maxspeed);
            localVel.x -= localVel.x * turnStrength;
            rg.velocity = transform.TransformDirection(localVel); // 월드 속도로 변경
            Vector3 localRot = transform.localRotation.eulerAngles;
            localRot.z = Mathf.Clamp((distGroundR - distGroundL) * 100, -20f, 20f); // 기울어진 정도
            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(localRot), Time.deltaTime * 10);
            if (jumpRequested)
            {
                rg.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                jumpRequested = false;
                isJumping = true;
            }
        }
        else if (!isGrounded && !isDead)
        {
            //On Air
            float airTlit = tilt;
            transform.Rotate(Vector3.up, airTlit * rotationSpeed * Time.deltaTime, Space.Self);
            float slip = airMoveAction.ReadValue<Vector2>().y;
            transform.Rotate(Vector3.right, slip * rotationSpeed * Time.deltaTime, Space.Self);
                if (rg.velocity.y > 0)
                {
                    Physics.gravity = new Vector3(0, reduceGravity, 0);
                }
                else if (rg.velocity.y < 0)
                {
                    Physics.gravity = new Vector3(0, changeGravity, 0);
                }
            
        }
    }

    #endregion

    #region REVIVE AND RESTART

    public void PlayerPressP(InputAction.CallbackContext callbackContext)
    {
        if (callbackContext.started)
        {
            restartAction.Enable();
        }
        else if (callbackContext.canceled)
        {
            restartAction.Disable();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Ground")) // Slope 태그와 충돌
        {

            if(isDead)
                return;
            isDead = true;

            infoText.text = "Press R / Start to Revive";

            //Canvas.transform.Find("YouDied").gameObject.SetActive(true); [UI CHANGE]
            CurTransform = rg.position; //죽은 위치 체크

            groundMoveAction.Disable();
            airMoveAction.Disable();
            jumpAction.Disable();
            restartAction.Disable();
            reviveAction.Enable();

            reviveAction.performed += PressRToRevive;
        }

        if(other.gameObject.CompareTag("Restart"))
        {
            infoText.text = "Press R / Start to Restart";

            reviveAction.Disable();
            groundMoveAction.Disable();
            airMoveAction.Disable();
            jumpAction.Disable();
            restartAction.Enable();

            restartAction.performed += PressRToRestart;
        }
    }

    void PressRToRevive(InputAction.CallbackContext callbackContext)
    {
       if (isDead)
       {
            isDead = false;
            reviveAction.performed -= PressRToRevive;
            string currentSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(currentSceneName);
       }

    }

    void PressRToRestart(InputAction.CallbackContext callbackContext)
    {
        restartAction.performed -= PressRToRestart;
        string currentSceneName = SceneManager.GetActiveScene().name;
       SceneManager.LoadScene(currentSceneName);
    }

    #endregion

    #region void ONJUMP

    void OnJump(InputAction.CallbackContext ctx)
    {
        if(ctx.performed && isGrounded)
        {
            jumpRequested = true;
        }
    }

    #endregion

    #region 경사면의 법선 벡터에 맞추기
    float CollisionTriggerDeltaTime = 1.5f;
    float CollisionExitDeltaTime = 0f;
    RaycastHit[] hits;
    Coroutine coroutine;
    bool CollisionExit = true;

    private void OnCollisionEnter(Collision collision)
    {
        CollisionExit = false; // 코루틴 종료를 위한 boolean 변경
        if (collision.gameObject.CompareTag("Ground") && CollisionTriggerDeltaTime > 1.1f && CollisionExitDeltaTime > 1f && !isDead)
        {
            Vector3 collisionUp = new Vector3(404f, 404f, 404f);
            Vector3 collisionPos = new Vector3(404f, 404f, 404f);
            hits = Physics.RaycastAll(transform.position, Vector3.down);
            for (int i = 0; i < hits.Length; i++)
            {
                if (!hits[i].transform.CompareTag("Ground"))
                    continue;
                collisionUp = hits[i].normal;
                collisionPos = hits[i].point;
                break;
            }
            if (collisionPos.x == 404f && collisionPos.y == 404f && collisionPos.z == 404f)
                return;

            Quaternion rot = Quaternion.FromToRotation(transform.up.normalized, collisionUp) * transform.rotation;

            if (coroutine != null)
                StopCoroutine(coroutine);
            coroutine = StartCoroutine(RotateForSafeLanding(rot));
            CollisionTriggerDeltaTime = 0f;
        }
        CollisionExitDeltaTime = 0f; // 값 초기화
    }

    private void OnCollisionExit(Collision collision)
    {
        CollisionExit = true; // 탈출 시 탈출 boolean 값 true
        StartCoroutine("CheckExitTime");
    }

    IEnumerator CheckExitTime()
    {
        while(CollisionExit) // collision 진입 시 코루틴 종료
        {
            CollisionExitDeltaTime += Time.unscaledDeltaTime; // collision 탈출 상태일 때 시간 누적
            yield return null;
        }
    }

    IEnumerator RotateForSafeLanding(Quaternion rot) // 경사면 도착하면 해당 법선벡터로 up 벡터를 맞춰 회전
    {
        float StartTime = Time.time;
        float elapsed = 0f;
        float t;
        Quaternion curRot;
        while (true)
        {
            elapsed += Time.unscaledDeltaTime;
            t = Mathf.Clamp01(elapsed / 3f); // 회전이 완료되는데 최대 1초에 대해 회전을 진행한다
            if (elapsed > 1.2f) // 
                break;
            curRot = transform.rotation;

            /*if (Mathf.Abs(curRot.eulerAngles.x - rot.eulerAngles.x) < 0.05f 
                || Mathf.Abs(curRot.eulerAngles.y - rot.eulerAngles.y) < 0.05f 
                || Mathf.Abs(curRot.eulerAngles.z - rot.eulerAngles.z) < 0.05f) // 각도 차이가 많지 않으면 거기서 회전 종료 << 체크 필요
                break;*/

            if (Quaternion.Angle(curRot, rot) < 5f)
                break;
            transform.rotation = Quaternion.Lerp(curRot, rot, t);
            yield return null;
        }
    }
    #endregion
}
