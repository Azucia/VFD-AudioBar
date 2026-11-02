using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class VFD_Ultimate : UdonSharpBehaviour
{
    [Header("目标音频源 (直接读取)")]
    [Tooltip("把你播放音乐的 Audio Source 拖进这里 (比如 VizVid 的主音响)")]
    public AudioSource targetAudio;

    [Header("左声道 (Left Channel)")]
    public Transform leftBody;
    public Transform leftCap;

    [Header("右声道 (Right Channel)")]
    public Transform rightBody;
    public Transform rightCap;

    [Header("音量与视觉控制")]
    [Tooltip("全频段整体放大器 (如果钢琴曲声音轻，把这个拉大到 15-30)")]
    public float masterGain = 15.0f;

    [Tooltip("最大高度 (米)")]
    public float maxHeight = 5.0f;

    [Tooltip("动态指数曲线 (推荐1.5。能让高音爆发更猛烈，低音起伏更平滑)")]
    public float visualCurve = 1.5f;

    [Header("物理沾滞感 (粘性手感)")]
    public float riseSpeed = 25.0f;  // 爆发上升速度 (建议快)
    public float fallSpeed = 5.0f;   // 阻尼回落速度 (这就是沾滞感来源，建议慢)
    public float gravity = 3.0f;     // 顶部掉落重力
    public float peakHoldTime = 0.5f; // 顶部悬停时间

    // --- 内部变量 ---
    private float[] _samplesLeft = new float[64];
    private float[] _samplesRight = new float[64];

    private float _currentLeftPeak = 0f;
    private float _currentRightPeak = 0f;
    private float _leftHoldTimer = 0f;
    private float _rightHoldTimer = 0f;
    private float _capOffset = 0f;

    void Start()
    {
        // 自动计算帽子的厚度偏移
        if (leftCap != null) _capOffset = leftCap.localScale.y * 0.5f;
    }

    void Update()
    {
        // 如果没拖音频源，或者没播放，就不动
        if (targetAudio == null || !targetAudio.isPlaying) return;

        float dt = Time.deltaTime;

        // 获取最原始的全频段左右声道波形 (0是左，1是右)
        targetAudio.GetOutputData(_samplesLeft, 0);
        targetAudio.GetOutputData(_samplesRight, 1);

        // 计算总音量 (RMS)，并乘以放大器
        float leftVol = GetVolume(_samplesLeft) * masterGain;
        float rightVol = GetVolume(_samplesRight) * masterGain;

        // 防呆：如果是单声道音乐，右耳复制左耳
        if (rightVol == 0 && leftVol > 0) rightVol = leftVol;

        // 限制在0-1之间，并应用指数曲线让视觉更有层次感
        leftVol = Mathf.Pow(Mathf.Clamp(leftVol, 0f, 1f), visualCurve);
        rightVol = Mathf.Pow(Mathf.Clamp(rightVol, 0f, 1f), visualCurve);

        // 换算成最终高度
        float leftTarget = leftVol * maxHeight;
        float rightTarget = rightVol * maxHeight;

        // 应用物理动画
        ProcessChannel(leftBody, leftCap, leftTarget, ref _currentLeftPeak, ref _leftHoldTimer, dt);
        ProcessChannel(rightBody, rightCap, rightTarget, ref _currentRightPeak, ref _rightHoldTimer, dt);
    }

    // 提取纯音量的算法 (RMS均方根)
    private float GetVolume(float[] samples)
    {
        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += samples[i] * samples[i];
        }
        return Mathf.Sqrt(sum / samples.Length);
    }

    // 伸缩与物理计算核心
    private void ProcessChannel(Transform body, Transform cap, float targetH, ref float currentPeak, ref float holdTimer, float dt)
    {
        if (body == null || cap == null) return;

        Vector3 bodyScale = body.localScale;
        float safeTarget = Mathf.Max(0.01f, targetH);

        // 【核心沾滞物理】：目标比当前高就爆发生长，目标比当前低就黏性回落
        float currentSpeed = (safeTarget > bodyScale.y) ? riseSpeed : fallSpeed;

        float newBodyH = Mathf.Lerp(bodyScale.y, safeTarget, dt * currentSpeed);
        body.localScale = new Vector3(bodyScale.x, newBodyH, bodyScale.z);

        // 帽子悬停与掉落逻辑
        if (newBodyH > currentPeak)
        {
            currentPeak = newBodyH;
            holdTimer = peakHoldTime;
        }
        else
        {
            if (holdTimer > 0) holdTimer -= dt;
            else currentPeak -= gravity * dt;

            if (currentPeak < newBodyH) currentPeak = newBodyH;
        }

        cap.localPosition = new Vector3(0, currentPeak + _capOffset, 0);
    }
}