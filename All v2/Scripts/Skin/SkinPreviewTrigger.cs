using UnityEngine;

/// <summary>
/// ������� ��� ������������� ����� ��� ��� ���������
/// </summary>
public class SkinPreviewTrigger : MonoBehaviour
{
    [Header("Preview Settings")]
    [SerializeField] private string previewSkinId = "";
    [SerializeField] private float previewDuration = 5f;
    [SerializeField] private bool autoRevert = true;

    [Header("Visual Feedback")]
    [SerializeField] private bool showPreviewEffect = true;
    [SerializeField] private ParticleSystem previewEffect;
    [SerializeField] private string previewMessage = "Previewing skin...";
    [SerializeField] private string revertMessage = "Skin preview ended";

    private string originalSkinId = "";
    private bool isPreviewing = false;
    private float previewStartTime = 0f;

    #region Unity Lifecycle

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isPreviewing)
        {
            StartSkinPreview(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && isPreviewing)
        {
            StopSkinPreview();
        }
    }

    private void Update()
    {
        // �������������� ���������� ������������� �� �������
        if (isPreviewing && autoRevert && Time.time - previewStartTime >= previewDuration)
        {
            StopSkinPreview();
        }
    }

    #endregion

    #region Preview Logic

    /// <summary>
    /// ������ ������������� �����
    /// </summary>
    private void StartSkinPreview(GameObject player)
    {
        if (!SkinManager.HasInstance || string.IsNullOrEmpty(previewSkinId)) return;

        var skinManager = SkinManager.Instance;
        var previewSkin = skinManager.GetSkin(previewSkinId);

        if (previewSkin == null)
        {
            Debug.LogWarning($"[SkinPreviewTrigger] Preview skin {previewSkinId} not found");
            return;
        }

        // ��������� ������������ ����
        var equippedSkin = skinManager.GetEquippedSkin();
        originalSkinId = equippedSkin?.skinId ?? "";

        // ��������� ���� ��� �������������
        skinManager.EquipSkin(previewSkinId);
        isPreviewing = true;
        previewStartTime = Time.time;

        PlayPreviewEffects();
        ShowPreviewMessage(true);

        Debug.Log($"[SkinPreviewTrigger] Previewing skin: {previewSkin.skinName}");
    }

    /// <summary>
    /// ���������� ������������� �����
    /// </summary>
    private void StopSkinPreview()
    {
        if (!isPreviewing || !SkinManager.HasInstance) return;

        // ���������� ������������ ����
        if (!string.IsNullOrEmpty(originalSkinId))
        {
            SkinManager.Instance.EquipSkin(originalSkinId);
        }

        isPreviewing = false;
        ShowPreviewMessage(false);

        Debug.Log("[SkinPreviewTrigger] Skin preview ended");
    }

    #endregion

    #region Effects

    /// <summary>
    /// ������������ �������� �������������
    /// </summary>
    private void PlayPreviewEffects()
    {
        if (!showPreviewEffect) return;

        if (previewEffect != null)
        {
            previewEffect.Play();
        }
    }

    #endregion

    #region UI Feedback

    /// <summary>
    /// ����� ��������� �������������
    /// </summary>
    private void ShowPreviewMessage(bool isStarting)
    {
        string message = isStarting ? previewMessage : revertMessage;
        // UIManager.Instance.ShowMessage(message);
        Debug.Log($"[SkinPreviewTrigger] {message}");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// ��������� ����� ��� �������������
    /// </summary>
    public void SetPreviewSkin(string skinId)
    {
        previewSkinId = skinId;
    }

    /// <summary>
    /// ������ ������ �������������
    /// </summary>
    public void StartPreviewManually(GameObject player = null)
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        if (player != null)
        {
            StartSkinPreview(player);
        }
    }

    /// <summary>
    /// ������ ���������� �������������
    /// </summary>
    public void StopPreviewManually()
    {
        StopSkinPreview();
    }

    #endregion
}