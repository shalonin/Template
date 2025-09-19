using UnityEngine;

/// <summary>
/// ���� ������ � ������� � ����������� ���������
/// </summary>
public class SkinRewardZone : MonoBehaviour
{
    [Header("Skin Reward")]
    [SerializeField] private string rewardSkinId = "";
    [SerializeField] private string zoneName = "Mystery Skin";

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem ambientEffect;
    [SerializeField] private Light zoneLight;
    [SerializeField] private Renderer zoneRenderer;
    [SerializeField] private Material activeMaterial;
    [SerializeField] private Material inactiveMaterial;

    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private string interactionPrompt = "Press E to claim skin";

    [Header("UI")]
    [SerializeField] private bool showInteractionPrompt = true;

    private bool playerInRange = false;
    private GameObject playerInZone;
    private bool isClaimed = false;
    private SkinManager.SkinData rewardSkin;

    #region Unity Lifecycle

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        HandleInteraction();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isClaimed)
        {
            playerInRange = true;
            playerInZone = other.gameObject;
            ShowInteractionPrompt(true);
            Debug.Log($"[SkinRewardZone] Press {interactionKey} to claim {zoneName}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            playerInZone = null;
            ShowInteractionPrompt(false);
        }
    }

    #endregion

    #region Initialization

    private void Initialize()
    {
        if (SkinManager.HasInstance)
        {
            rewardSkin = SkinManager.Instance.GetSkin(rewardSkinId);
        }

        UpdateVisualState();
    }

    #endregion

    #region Interaction

    /// <summary>
    /// ��������� ��������������
    /// </summary>
    private void HandleInteraction()
    {
        if (playerInRange && Input.GetKeyDown(interactionKey) && !isClaimed)
        {
            ClaimReward();
        }
    }

    /// <summary>
    /// ��������� �������
    /// </summary>
    private void ClaimReward()
    {
        if (!SkinManager.HasInstance || isClaimed) return;

        var skinManager = SkinManager.Instance;
        var skinData = skinManager.GetSkin(rewardSkinId);

        if (skinData == null)
        {
            Debug.LogWarning($"[SkinRewardZone] Reward skin {rewardSkinId} not found");
            return;
        }

        // ������������ � ��������� ����
        skinManager.UnlockSkin(rewardSkinId);
        skinManager.EquipSkin(rewardSkinId);

        isClaimed = true;
        playerInRange = false;

        // ������� ��������� �������
        PlayClaimEffects();
        ShowRewardNotification(skinData);
        UpdateVisualState();

        Debug.Log($"[SkinRewardZone] Claimed skin: {skinData.skinName}");
    }

    #endregion

    #region Effects

    /// <summary>
    /// ������������ �������� ��������� �������
    /// </summary>
    private void PlayClaimEffects()
    {
        // ������� �����
        if (zoneLight != null)
        {
            StartCoroutine(LightFlashEffect());
        }

        // ��������� �����
        if (zoneRenderer != null && inactiveMaterial != null)
        {
            zoneRenderer.material = inactiveMaterial;
        }

        // ������ ������
        if (ambientEffect != null)
        {
            var claimEffect = Instantiate(ambientEffect, transform.position, Quaternion.identity);
            claimEffect.Play();
            Destroy(claimEffect.gameObject, claimEffect.main.duration);
        }
    }

    /// <summary>
    /// ������ ������� �����
    /// </summary>
    private System.Collections.IEnumerator LightFlashEffect()
    {
        if (zoneLight == null) yield break;

        float originalIntensity = zoneLight.intensity;
        Color originalColor = zoneLight.color;

        // ������� �������
        zoneLight.intensity = originalIntensity * 3f;
        zoneLight.color = Color.white;

        yield return new WaitForSeconds(0.1f);

        // ������� �������
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            zoneLight.intensity = Mathf.Lerp(originalIntensity * 3f, originalIntensity, t);
            zoneLight.color = Color.Lerp(Color.white, originalColor, t);
            yield return null;
        }
    }

    #endregion

    #region Visual State

    /// <summary>
    /// ���������� ����������� ��������� ����
    /// </summary>
    private void UpdateVisualState()
    {
        if (zoneRenderer != null)
        {
            zoneRenderer.material = isClaimed ? inactiveMaterial : activeMaterial;
        }

        if (zoneLight != null)
        {
            zoneLight.enabled = !isClaimed;
        }

        if (ambientEffect != null)
        {
            if (isClaimed)
            {
                ambientEffect.Stop();
            }
            else
            {
                ambientEffect.Play();
            }
        }
    }

    #endregion

    #region UI Feedback

    /// <summary>
    /// ����� ��������� ��������������
    /// </summary>
    private void ShowInteractionPrompt(bool show)
    {
        if (!showInteractionPrompt) return;

        // ����� ������ ���� ���������� � ����� UI ��������
        // ��������: UIManager.Instance.ShowPrompt(show ? interactionPrompt : "");

        if (show)
        {
            Debug.Log($"[SkinRewardZone] {interactionPrompt}");
        }
    }

    /// <summary>
    /// ����� ����������� � �������
    /// </summary>
    private void ShowRewardNotification(SkinManager.SkinData skinData)
    {
        string message = $"You claimed {skinData.skinName}!";
        // UIManager.Instance.ShowNotification(message, 3f);
        Debug.Log($"[SkinRewardZone] {message}");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// ��������� ���������� �����
    /// </summary>
    public void SetRewardSkin(string skinId)
    {
        rewardSkinId = skinId;
        isClaimed = false;
        Initialize();
    }

    /// <summary>
    /// ������ ��������� �������
    /// </summary>
    public void ClaimRewardManually()
    {
        ClaimReward();
    }

    #endregion
}