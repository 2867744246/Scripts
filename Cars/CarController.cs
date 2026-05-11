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

    [Header("材质引用")]
    public Renderer brakeLightRenderer;
    [Tooltip("尾灯材质在Renderer中的索引（如果有多个材质）")]
    public int brakeLightMaterialIndex = 1;

    [Header("车辆物理参数")]
    public float maxMotorTorque = 800f;
    public float maxSteerAngle = 30f;
    public float brakeTorque = 2000f;
    [Tooltip("最大速度限制 (km/h)")]
    public float maxSpeed = 150f; // km/h
    [Tooltip("最低速度限制，低于该速度时车辆自动起步并阻止刹车将速度降得更低")]
    public float minSpeed = 10f; // km/h
    
    [Header("UI 引用")]
    [Tooltip("可选：用于显示速度的 Text 组件")]
    public Text speedText;
    [Tooltip("是否显示单位")]
    public bool showUnit = true;

    [Header("移动输入（可选，移动端按钮）")]
    public MobileInputManager mobileInput;

    // 已解析过移动端输入引用，避免重复查找
    private bool mobileInputResolved;

    // 私有控制变量
    private Rigidbody rb;
    private float currentMotorTorque; // 当前平滑后的扭矩
    private float currentSpeed; // 当前速度 (km/h)
    private MaterialPropertyBlock brakeLightBlock;
    private float lastFramePositionX; // 上一帧 X 坐标，用于计算行驶距离
    private float distanceTraveled; // 累计行驶距离 (m)
    private bool positionInitialized; // 是否已初始化位置记录

    /// <summary>
    /// 获取当前速度（只读）
    /// </summary>
    public float CurrentSpeed => currentSpeed;

    /// <summary>
    /// 获取累计行驶距离（单位：米）
    /// </summary>
    public float DistanceTraveled => distanceTraveled;

    /// <summary>
    /// 车辆发生碰撞时触发的事件。
    /// </summary>
    public System.Action onCollision;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // 1. 设置一个更低、更靠前的重心，大幅提升稳定性
        rb.centerOfMass = new Vector3(0, -0.7f, -0.3f);
        // 增加角阻尼以减少旋转抖动
        rb.angularDrag = 0.5f;

        // 3. 初始化尾灯MaterialPropertyBlock
        if (brakeLightRenderer != null)
        {
            brakeLightBlock = new MaterialPropertyBlock();
        }

        // 4. 确保最低速度合理
        if (minSpeed < 0f)
        {
            minSpeed = 0f;
        }

        // 尝试获取移动端输入对象，若 Inspector 未手动分配则自动查找
        GetMobileInputManager();
    }

    /// <summary>
    /// 获取当前关联的移动端输入管理器。
    /// 如果尚未分配，则在场景中查找第一个 MobileInputManager 实例并缓存。
    /// </summary>
    private void GetMobileInputManager()
    {
        if (mobileInput == null)
            {
                mobileInput = FindObjectOfType<MobileInputManager>();
                if (mobileInput == null)
                {
                    Debug.LogWarning("CarController: 未在场景中找到 MobileInputManager 实例，移动端输入将不可用。");
                }
            }
    }


    void FixedUpdate()
    {
        // 1. 获取输入
        GetInput(out float verticalInput, out float horizontalInput, out bool isBrake);

        // 2. 应用扭矩和速度限制
        ApplyMotorTorque(verticalInput);

        // 3. 应用转向
        ApplySteering(horizontalInput);

        // 4. 处理刹车
        ApplyBraking(isBrake);

        // 5. 累加行驶距离
        UpdateDistanceTraveled();

        // 6. 更新 UI 显示
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
    /// 获取输入（优先使用移动输入管理器，否则回退到键盘）
    /// </summary>
    void GetInput(out float verticalInput, out float horizontalInput, out bool isBrake)
    {
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
    }

    /// <summary>
    /// 应用扭矩和速度限制
    /// </summary>
    void ApplyMotorTorque(float verticalInput)
    {
        // 计算当前速度并准备扭矩
        currentSpeed = rb.velocity.magnitude * 3.6f; // m/s -> km/h
        float targetTorque = verticalInput * maxMotorTorque;

        // 自动起步：如果速度低于最低速度且玩家没有主动加油，则给一个小扭矩使车辆自行启动
        if (currentSpeed < minSpeed && verticalInput <= 0f)
        {
            float autoStartTorque = maxMotorTorque * 1f;
            targetTorque = Mathf.Max(targetTorque, autoStartTorque);
        }

        currentMotorTorque = targetTorque;

        // 速度限制逻辑：如果达到最大速度且继续加速，则减少扭矩
        if (currentSpeed >= maxSpeed && verticalInput > 0f)
        {
            // 当达到最大速度时，只允许维持速度，不允许继续加速
            currentMotorTorque = Mathf.Min(currentMotorTorque, rb.drag * maxSpeed / 3.6f);
        }

        // 应用四轮驱动
        frontLeftWheelCollider.motorTorque = currentMotorTorque;
        frontRightWheelCollider.motorTorque = currentMotorTorque;
        rearLeftWheelCollider.motorTorque = currentMotorTorque;
        rearRightWheelCollider.motorTorque = currentMotorTorque;
    }

    /// <summary>
    /// 应用转向
    /// </summary>
    void ApplySteering(float horizontalInput)
    {
        float steerAngle = horizontalInput * maxSteerAngle;
        frontLeftWheelCollider.steerAngle = steerAngle;
        frontRightWheelCollider.steerAngle = steerAngle;
    }

    /// <summary>
    /// 处理刹车逻辑
    /// </summary>
    void ApplyBraking(bool isBrake)
    {
        float brake = 0f;

        // 低于最低速度时禁止刹车，使车辆保持最低速度
        if (isBrake && currentSpeed > minSpeed)
        {
            brake = brakeTorque;
        }

        frontLeftWheelCollider.brakeTorque = brake;
        frontRightWheelCollider.brakeTorque = brake;
        rearLeftWheelCollider.brakeTorque = brake;
        rearRightWheelCollider.brakeTorque = brake;

        // 控制尾灯Emission
        if (brakeLightRenderer != null && brakeLightBlock != null)
        {
            brakeLightBlock.SetColor("_EmissionColor", isBrake ? Color.white : Color.black);
            brakeLightRenderer.SetPropertyBlock(brakeLightBlock, brakeLightMaterialIndex);
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

        // 新逻辑（直接同步世界变换）：视觉模型与 WheelCollider 已分离，安全设置世界位置/旋转
        model.position = pos;
        model.rotation = rot;

        // 叠加轮子自转（绕本地 X 轴），使用 rpm -> deg/s = rpm*6
        float degPerSec = collider.rpm * 6f;
        model.Rotate(Vector3.right, degPerSec * Time.deltaTime, Space.Self);
    }

    /// <summary>
    /// 累加行驶距离（沿 X 轴方向）。
    /// 第一帧初始化上一帧位置，之后按 X 轴增量累加距离。
    /// </summary>
    private void UpdateDistanceTraveled()
    {
        float currentX = transform.position.x;

        if (!positionInitialized)
        {
            lastFramePositionX = currentX;
            positionInitialized = true;
            return;
        }

        float deltaX = currentX - lastFramePositionX;
        // 只累加正向移动（防止倒车减少距离）
        if (deltaX > 0f)
        {
            distanceTraveled += deltaX;
        }
        lastFramePositionX = currentX;
    }

    /// <summary>
    /// 碰撞检测回调，触发结算事件。
    /// </summary>
    /// <param name="collision">碰撞信息。</param>
    private void OnCollisionEnter(Collision collision)
    {
        onCollision?.Invoke();
    }
}
