using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("车轮碰撞体引用")]
    public WheelCollider frontLeftWheelCollider;
    public WheelCollider frontRightWheelCollider;
    public WheelCollider rearLeftWheelCollider;
    public WheelCollider rearRightWheelCollider;

    [Header("轮子模型引用（用于同步旋转）")]
    public Transform frontLeftWheelModel;
    public Transform frontRightWheelModel;
    public Transform rearLeftWheelModel;
    public Transform rearRightWheelModel;

    [Header("车辆物理参数")]
    public float maxMotorTorque = 800f;
    public float maxSteerAngle = 30f;
    public float brakeTorque = 2000f;
    [Tooltip("最大速度限制 (km/h)")]
    public float maxSpeed = 150f; // km/h
    
    [Header("UI 引用")]
    [Tooltip("可选：用于显示速度的 Text 组件")]
    public Text speedText;
    [Tooltip("是否显示单位")]
    public bool showUnit = true;

    [Header("高级控制")]
    [Tooltip("扭矩平滑变化速度")]
    public float torqueRampSpeed = 2f;
    [Tooltip("防倾翻稳定系数")]
    public float antiRollForce = 5000f;

    [Header("移动输入（可选，移动端按钮/摇杆）")]
    public MobileInputManager mobileInput;

    // 私有控制变量
    private Rigidbody rb;
    private float currentMotorTorque; // 当前平滑后的扭矩
    private float currentSpeed; // 当前速度 (km/h)

    /// <summary>
    /// 获取当前速度（只读）
    /// </summary>
    public float CurrentSpeed => currentSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // 1. 设置一个更低、更靠前的重心，大幅提升稳定性
        rb.centerOfMass = new Vector3(0, -0.7f, -0.3f);
        
        // 2. 初始化所有车轮悬挂，防止弹跳
        //InitializeAllWheelColliders();
    }

    void FixedUpdate()
    {

        // 1. 获取输入（优先使用移动输入管理器，否则回退到键盘）
        float verticalInput;
        float horizontalInput;
        bool isBrake;

        // 优化输入读取：先缓存键盘输入，优先使用有效的键盘值，否则使用移动输入（仅当存在且有效）
        float kbVertical = Input.GetAxis("Vertical");
        float kbHorizontal = Input.GetAxis("Horizontal");
        bool kbBrake = Input.GetKey(KeyCode.Space);

        const float EPS = 0.001f; // 阈值，避免浮点噪声导致判断失灵

        if (Mathf.Abs(kbVertical) > EPS)
            verticalInput = kbVertical;
        else if (mobileInput != null && Mathf.Abs(mobileInput.throttle) > EPS)
            verticalInput = mobileInput.throttle;
        else
            verticalInput = 0f;

        if (Mathf.Abs(kbHorizontal) > EPS)
            horizontalInput = kbHorizontal;
        else if (mobileInput != null && Mathf.Abs(mobileInput.steer) > EPS)
            horizontalInput = mobileInput.steer;
        else
            horizontalInput = 0f;

        isBrake = kbBrake || (mobileInput != null && mobileInput.handbrake);
        
        // 2. 平滑扭矩计算 (解决动力突增导致的抬头/打滑)
        float targetTorque = verticalInput * maxMotorTorque;
        //currentMotorTorque = Mathf.Lerp(currentMotorTorque, targetTorque, Time.fixedDeltaTime * torqueRampSpeed);
        currentMotorTorque = targetTorque;

        // 2.5. 速度限制逻辑：如果达到最大速度且继续加速，则减少扭矩
        currentSpeed = rb.velocity.magnitude * 3.6f; // m/s -> km/h
        
        if (currentSpeed >= maxSpeed && verticalInput > 0)
        {
            // 当达到最大速度时，只允许维持速度，不允许继续加速
            currentMotorTorque = Mathf.Min(currentMotorTorque, rb.drag * maxSpeed / 3.6f);
        }

        // 3. 应用后轮驱动
        rearLeftWheelCollider.motorTorque = currentMotorTorque;
        rearRightWheelCollider.motorTorque = currentMotorTorque;

        // 4. 应用前轮转向
        float steerAngle = horizontalInput * maxSteerAngle;
        frontLeftWheelCollider.steerAngle = steerAngle;
        frontRightWheelCollider.steerAngle = steerAngle;

        // 5. 处理刹车
        ApplyBraking(isBrake);

        // 6. 应用防侧倾稳定 (可选，让过弯更稳定)
        //ApplyAntiRollBar(frontLeftWheelCollider, frontRightWheelCollider);
        //ApplyAntiRollBar(rearLeftWheelCollider, rearRightWheelCollider);
        
        // 7. 更新 UI 显示
        UpdateSpeedUI();
    }

    void LateUpdate()
    {
        // 同步所有车轮模型的位置和旋转
        UpdateWheelModel(frontLeftWheelCollider, frontLeftWheelModel);
        UpdateWheelModel(frontRightWheelCollider, frontRightWheelModel);
        UpdateWheelModel(rearLeftWheelCollider, rearLeftWheelModel);
        UpdateWheelModel(rearRightWheelCollider, rearRightWheelModel);

        // 更新速度显示
        if (speedText != null)
        {
            speedText.text = $"{currentSpeed:0.00}" + (showUnit ? " km/h" : "");
        }
    }

    /// <summary>
    /// 停用
    /// 初始化单个WheelCollider的悬挂参数，这是稳定性的基础
    /// </summary>
    void InitializeWheelCollider(WheelCollider wc)
    {
        if (wc == null) return;
        
        JointSpring suspensionSpring = wc.suspensionSpring;
        suspensionSpring.spring = 45000f;      // 悬挂刚度
        suspensionSpring.damper = 5000f;       // 阻尼系数
        suspensionSpring.targetPosition = 0.35f; // 目标压缩位置，模拟车重下压
        wc.suspensionSpring = suspensionSpring;
        wc.suspensionDistance = 0.2f;          // 悬挂可移动总距离
    }

    /// <summary>
    /// 初始化所有四个车轮的悬挂
    /// </summary>
    void InitializeAllWheelColliders()
    {
        InitializeWheelCollider(frontLeftWheelCollider);
        InitializeWheelCollider(frontRightWheelCollider);
        InitializeWheelCollider(rearLeftWheelCollider);
        InitializeWheelCollider(rearRightWheelCollider);
    }

    /// <summary>
    /// 处理刹车逻辑
    /// </summary>
    void ApplyBraking(bool isBrake)
    {
        float brake = isBrake ? brakeTorque : 0f;
        frontLeftWheelCollider.brakeTorque = brake;
        frontRightWheelCollider.brakeTorque = brake;
        rearLeftWheelCollider.brakeTorque = brake;
        rearRightWheelCollider.brakeTorque = brake;
    }

    /// <summary>
    /// 简单的防倾杆模拟，在过弯时增加稳定性
    /// </summary>
    void ApplyAntiRollBar(WheelCollider leftWheel, WheelCollider rightWheel)
    {
        if (leftWheel == null || rightWheel == null) return;

        WheelHit hit;
        float leftTravel = 1f;
        float rightTravel = 1f;

        bool leftGrounded = leftWheel.GetGroundHit(out hit);
        if (leftGrounded)
        {
            leftTravel = (-leftWheel.transform.InverseTransformPoint(hit.point).y - leftWheel.radius) / leftWheel.suspensionDistance;
        }

        bool rightGrounded = rightWheel.GetGroundHit(out hit);
        if (rightGrounded)
        {
            rightTravel = (-rightWheel.transform.InverseTransformPoint(hit.point).y - rightWheel.radius) / rightWheel.suspensionDistance;
        }

        float antiRollForceDelta = (leftTravel - rightTravel) * antiRollForce;

        if (leftGrounded)
        {
            rb.AddForceAtPosition(leftWheel.transform.up * -antiRollForceDelta, leftWheel.transform.position);
        }
        if (rightGrounded)
        {
            rb.AddForceAtPosition(rightWheel.transform.up * antiRollForceDelta, rightWheel.transform.position);
        }
    }

    /// <summary>
    /// 更新速度 UI 显示
    /// </summary>
    void UpdateSpeedUI()
    {
        if (speedText != null)
        {
            if (showUnit)
                speedText.text = $"{currentSpeed:F0} km/h";
            else
                speedText.text = $"{currentSpeed:F0}";
        }
    }

    /// <summary>
    /// 同步轮子视觉模型与物理碰撞体的位置和旋转
    /// </summary>
    void UpdateWheelModel(WheelCollider collider, Transform model)
    {
        if (collider == null || model == null) return;

        Vector3 pos;
        Quaternion rot;
        collider.GetWorldPose(out pos, out rot);
        // 简化逻辑：视觉模型与碰撞体已分离（推荐层级），直接同步世界位置与旋转。
        // 下面保留旧逻辑作为注释备份，以便回滚或调试。

        /*
        // 旧逻辑：根据模型与碰撞体的关系选择不同的同步策略
        bool sameObject = model == collider.transform || model.gameObject == collider.gameObject;
        bool modelIsChildOfCollider = model.IsChildOf(collider.transform);
        bool colliderIsChildOfModel = collider.transform.IsChildOf(model);

        if (sameObject || modelIsChildOfCollider || colliderIsChildOfModel)
        {
            // 只更新旋转（包括转向/悬挂带来的角度），不要写入位置
            model.rotation = rot;
        }
        else
        {
            // 标准情况：独立的视觉模型，更新位置与旋转
            model.position = pos;
            model.rotation = rot;
        }
        */

        // 新逻辑（直接同步世界变换）：视觉模型与 WheelCollider 已分离，安全设置世界位置/旋转
        model.position = pos;
        model.rotation = rot;

        // 叠加轮子自转（绕本地 X 轴），使用 rpm -> deg/s = rpm*6
        float degPerSec = collider.rpm * 6f;
        model.Rotate(Vector3.right, degPerSec * Time.deltaTime, Space.Self);
    }
}