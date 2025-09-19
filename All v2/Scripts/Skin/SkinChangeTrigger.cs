using UnityEngine;

/// <summary>
/// ������� ��� ������� ����� ����� ������
/// �������� � Collider ���������� (��� Rigidbody)
/// </summary>
public class SkinChangeTrigger : MonoBehaviour
{
    [Header("Skin Settings")]
    [SerializeField] private string targetSkinId = ""; // ID ����� ��� ����������
    [SerializeField] private bool autoUnlock = true;   // ������������� �������������� ����
    [SerializeField] private bool oneTimeUse = true;   // ����������� �������������
    [SerializeField] private bool requirePlayerTag = true; // ��������� ��� ������
    [SerializeField] private string playerTag = "Player";  // ��� ������

    [Header("Conditions")]
    [SerializeField] private bool requireQuestCompleted = false; // ��������� ���������� ������
    [SerializeField] private string requiredQuestId = "";
    [SerializeField] private bool requireLevel = false; // ��������� �������
    [SerializeField] private int requiredLevel = 1;

    [Header("Effects")]
    [SerializeField] private bool playEffect = true;
    [SerializeField] private ParticleSystem changeEffect;
    [SerializeField] private AudioClip changeSound;
    [SerializeField] private float effectDuration = 1f;

    [Header("UI Feedback")]
    [SerializeField] private bool showNotification = true;
    [SerializeField] private string successMessage = "New skin equipped!";
    [SerializeField] private string failMessage = "Cannot equip this skin!";

    private bool isUsed = false;
    private Collider triggerCollider;
    private AudioSource audioSource;

    #region Unity Lifecycle

    private void Start()
    {
        Initialize();
    }

    /// <summary>
    /// ����������� ��� ����� � �������
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log($"<Color=Green> >>> OnTriggerEnter </Color> {other.gameObject.tag}");
        if (CanTrigger(other))
        {
            ChangePlayerSkin(other.gameObject);

            if (oneTimeUse)
            {
                DeactivateTrigger();
            }
        }
    }


    /// <summary>
    /// ����������� ��� ������ �� �������� (�����������)
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        // ����� �������� ������ ��� ������ �� ��������
    }

    #endregion

    #region Initialization

    /// <summary>
    /// ������������� ��������
    /// </summary>
    private void Initialize()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true; // �������� ��� ��� �������
        }

        audioSource = GetComponent<AudioSource>();

        // ��������� ��������� ���� ��� �����
        if (string.IsNullOrEmpty(targetSkinId))
        {
            if (triggerCollider != null)
            {
                triggerCollider.enabled = false;
            }
            //Debug.LogWarning($"[SkinChangeTrigger] No skin ID set on {gameObject.name}");
        }

        //Debug.Log($"[SkinChangeTrigger] Initialized on {gameObject.name}");
    }

    #endregion

    #region Trigger Logic

    /// <summary>
    /// �������� ����������� ������������ ��������
    /// </summary>
    private bool CanTrigger(Collider other)
    {
        // �������� ������������ �������������
        if (isUsed && oneTimeUse)
        {
            //Debug.Log($"[SkinChangeTrigger] Trigger {gameObject.name} already used");
            return false;
        }

        // �������� ���� ������
        if (requirePlayerTag && !other.CompareTag(playerTag))
        {
            //Debug.Log($"[SkinChangeTrigger] Object {other.name} is not player");
            return false;
        }

        // �������� ������� ID �����
        if (string.IsNullOrEmpty(targetSkinId))
        {
            //Debug.LogWarning($"[SkinChangeTrigger] No skin ID set");
            return false;
        }

        // �������� ������� SkinManager
        if (!SkinManager.HasInstance)
        {
            //Debug.LogWarning($"[SkinChangeTrigger] SkinManager not found");
            return false;
        }

        // ��������� �������������� �������
        if (requireQuestCompleted && !IsQuestCompleted())
        {
            //Debug.Log($"[SkinChangeTrigger] Quest {requiredQuestId} not completed");
            return false;
        }

        if (requireLevel && !HasRequiredLevel())
        {
            //Debug.Log($"[SkinChangeTrigger] Level {requiredLevel} required");
            return false;
        }

        //Debug.Log($"[SkinChangeTrigger] Trigger conditions met for {other.name}");
        return true;
    }

    /// <summary>
    /// ����� ����� ������
    /// </summary>
    private void ChangePlayerSkin(GameObject player)
    {
        var skinManager = SkinManager.Instance;
        if (skinManager == null) return;

        // ��������� ���������� �� ����
        var skinData = skinManager.GetSkin(targetSkinId);
        if (skinData == null)
        {
            //Debug.LogWarning($"[SkinChangeTrigger] Skin {targetSkinId} not found");
            ShowFeedback(false);
            return;
        }

        // ������������ ���� ���� �����
        if (autoUnlock && !skinData.isUnlocked)
        {
            bool unlocked = skinManager.UnlockSkin(targetSkinId);
            //Debug.Log($"[SkinChangeTrigger] Skin {targetSkinId} unlocked: {unlocked}");
        }

        // ��������� ����
        bool success = skinManager.EquipSkin(targetSkinId);

        if (success)
        {
            PlayEffects();
            ShowFeedback(true);
            //Debug.Log($"[SkinChangeTrigger] Player skin changed to {skinData.skinName}");
        }
        else
        {
            ShowFeedback(false);
            //Debug.Log($"[SkinChangeTrigger] Failed to change player skin to {skinData.skinName}");
        }
    }

    /// <summary>
    /// �������� ���������� ������
    /// </summary>
    private bool IsQuestCompleted()
    {
        if (string.IsNullOrEmpty(requiredQuestId)) return true;

        // ����� ������ ���� ���������� � ����� �������� �������
        // ��������: return QuestManager.Instance.IsQuestCompleted(requiredQuestId);

        // ��� ������� ���������� PlayerPrefs
        return PlayerPrefs.GetInt($"Quest_{requiredQuestId}_Completed", 0) == 1;
    }

    /// <summary>
    /// �������� ������ ������
    /// </summary>
    private bool HasRequiredLevel()
    {
        // ����� ������ ���� ���������� � ����� �������� �������
        // ��������: return PlayerStats.Instance.Level >= requiredLevel;

        // ��� ������� ���������� PlayerPrefs
        int currentLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
        return currentLevel >= requiredLevel;
    }

    /// <summary>
    /// ����������� ��������
    /// </summary>
    private void DeactivateTrigger()
    {
        isUsed = true;

        // ��������� ���������� ��������
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }

        // ��������� ���������
        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }

        // ��������� ���� � ������ �������
        var light = GetComponent<Light>();
        if (light != null)
        {
            light.enabled = false;
        }

        //Debug.Log($"[SkinChangeTrigger] Trigger {gameObject.name} deactivated");
    }

    #endregion

    #region Effects

    /// <summary>
    /// ������������ ��������
    /// </summary>
    private void PlayEffects()
    {
        if (!playEffect) return;

        // ������ ������
        if (changeEffect != null)
        {
            changeEffect.Play();
            //Debug.Log("[SkinChangeTrigger] Particle effect played");
        }

        // ����
        if (changeSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(changeSound);
            //Debug.Log("[SkinChangeTrigger] Sound effect played");
        }

        // ����� �������� ������ �������
    }

    #endregion

    #region Feedback

    /// <summary>
    /// ����� �������� �����
    /// </summary>
    private void ShowFeedback(bool success)
    {
        if (!showNotification) return;

        string message = success ? successMessage : failMessage;
        // ����� ����� ������������� � ����� �������� �����������
        // ��������: NotificationManager.Instance.ShowMessage(message);

        //Debug.Log($"[SkinChangeTrigger] Feedback: {message}");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// ��������� ID �����
    /// </summary>
    public void SetTargetSkinId(string skinId)
    {
        targetSkinId = skinId;

        // ������������ ������� ���� �� ��� ��������
        if (isUsed && triggerCollider != null)
        {
            isUsed = false;
            triggerCollider.enabled = true;

            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }

        //Debug.Log($"[SkinChangeTrigger] Target skin ID set to {skinId}");
    }

    /// <summary>
    /// ������ ��������� ��������
    /// </summary>
    public void TriggerManually(GameObject player = null)
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag(playerTag);
        }

        if (player != null)
        {
            ChangePlayerSkin(player);
        }
    }

    /// <summary>
    /// ����� ��������
    /// </summary>
    public void ResetTrigger()
    {
        isUsed = false;

        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
        }

        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = true;
        }

        //Debug.Log($"[SkinChangeTrigger] Trigger {gameObject.name} reset");
    }

    /// <summary>
    /// �������� ����������� �� �������
    /// </summary>
    public bool IsUsed()
    {
        return isUsed;
    }

    #endregion
}