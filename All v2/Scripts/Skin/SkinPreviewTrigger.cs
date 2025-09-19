using UnityEngine;

/// <summary>
/// Триггер для предпросмотра скина без его получения
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
        // Автоматическое завершение предпросмотра по времени
        if (isPreviewing && autoRevert && Time.time - previewStartTime >= previewDuration)
        {
            StopSkinPreview();
        }
    }

    #endregion

    #region Preview Logic

    /// <summary>
    /// Начало предпросмотра скина
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

        // Сохраняем оригинальный скин
        var equippedSkin = skinManager.GetEquippedSkin();
        originalSkinId = equippedSkin?.skinId ?? "";

        // Применяем скин для предпросмотра
        skinManager.EquipSkin(previewSkinId);
        isPreviewing = true;
        previewStartTime = Time.time;

        PlayPreviewEffects();
        ShowPreviewMessage(true);

        Debug.Log($"[SkinPreviewTrigger] Previewing skin: {previewSkin.skinName}");
    }

    /// <summary>
    /// Завершение предпросмотра скина
    /// </summary>
    private void StopSkinPreview()
    {
        if (!isPreviewing || !SkinManager.HasInstance) return;

        // Возвращаем оригинальный скин
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
    /// Проигрывание эффектов предпросмотра
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
    /// Показ сообщения предпросмотра
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
    /// Установка скина для предпросмотра
    /// </summary>
    public void SetPreviewSkin(string skinId)
    {
        previewSkinId = skinId;
    }

    /// <summary>
    /// Ручной запуск предпросмотра
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
    /// Ручное завершение предпросмотра
    /// </summary>
    public void StopPreviewManually()
    {
        StopSkinPreview();
    }

    #endregion
}