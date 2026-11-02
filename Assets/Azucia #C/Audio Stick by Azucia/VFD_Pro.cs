using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class VFD_Pro : UdonSharpBehaviour
{
    [Header("AudioLink 核心")]
    // 兼容旧版的 AudioLink 引用格式
    public AudioLink.AudioLink audioLink;

    [Header("左声道 (Bass)")]
    public Transform leftBody; // 青色柱子的父物体
    public Transform leftCap;  // 红色帽子

    [Header("右声道 (Treble)")]
    public Transform rightBody; // 青色柱子的父物体
    public Transform rightCap;  // 红色帽子

    [Header("核心表现参数")]
    [Tooltip("最大高度 (米)")]
    public float maxHeight = 5.0f;

    [Tooltip("视觉动态曲线 (推荐 1.5 到 2.5)。解决'非0即满'的问题，让高潮爆发更明显，平时更平缓。")]
    public float visualCurve = 1.8f;

    [Header("物理沾滞感设置")]
    [Tooltip("上升速度 (爆发力，建议调高)")]
    public float riseSpeed = 25.0f;

    [Tooltip("下降速度 (沾滞感来源，建议调低，比如 5 到 8)")]
    public float fallSpeed = 6.0f;

    [Tooltip("帽子下落重力 (越大掉得越快)")]
    public float gravity = 3.0f;

    [Tooltip("悬停时间 (秒): 帽子在最高点停留的时间")]
    public float peakHoldTime = 0.5f;

    // --- 内部变量 ---
    private float _currentLeftPeak = 0f;
    private float _currentRightPeak = 0f;
    private float _leftHoldTimer = 0f;
    private float _rightHoldTimer = 0f;
    private float _capOffset = 0f;

    void Start()
    {
        if (leftCap != null) _capOffset = leftCap.localScale.y * 0.5f;
    }

    private void Update()
    {
        if (audioLink == null) return;

        float dt = Time.deltaTime;

        // 1. 获取原始音频数据 (使用 .grayscale 将颜色转为浮点数)
        float rawLeft = audioLink.audioData[0].grayscale;
        float rawRight = audioLink.audioData[3].grayscale;

        // 2. 施加“指数曲线”魔法，增加动态对比度
        rawLeft = Mathf.Pow(rawLeft, visualCurve);
        rawRight = Mathf.Pow(rawRight, visualCurve);

        // 3. 计算目标高度
        float leftTarget = rawLeft * maxHeight;
        float rightTarget = rawRight * maxHeight;

        // 处理左右声道
        ProcessChannel(leftBody, leftCap, leftTarget, ref _currentLeftPeak, ref _leftHoldTimer, dt);
        ProcessChannel(rightBody, rightCap, rightTarget, ref _currentRightPeak, ref _rightHoldTimer, dt);
    }

    private void ProcessChannel(Transform body, Transform cap, float targetH, ref float currentPeak, ref float holdTimer, float dt)
    {
        if (body == null || cap == null) return;

        // --- A. 柱子 (Body) 沾滞伸缩逻辑 ---
        float safeTarget = Mathf.Max(0.01f, targetH);
        Vector3 bodyScale = body.localScale;

        // 核心判断：如果目标比当前高，用爆发速度；如果比当前低，用沾滞回落速度
        float currentSpeed = (safeTarget > bodyScale.y) ? riseSpeed : fallSpeed;

        float newBodyH = Mathf.Lerp(bodyScale.y, safeTarget, dt * currentSpeed);
        body.localScale = new Vector3(bodyScale.x, newBodyH, bodyScale.z);

        // --- B. 帽子 (Cap) 物理悬停与掉落逻辑 ---
        if (newBodyH > currentPeak)
        {
            currentPeak = newBodyH;
            holdTimer = peakHoldTime;
        }
        else
        {
            if (holdTimer > 0)
            {
                holdTimer -= dt;
            }
            else
            {
                currentPeak -= gravity * dt;
            }

            // 兜底防穿模
            if (currentPeak < newBodyH)
            {
                currentPeak = newBodyH;
            }
        }

        // --- C. 应用帽子位置 ---
        cap.localPosition = new Vector3(0, currentPeak + _capOffset, 0);
    }
}