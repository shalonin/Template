using UnityEngine;

/// <summary>
/// ������ ��� ��������� ������ ��� ������ � ���������� ��������
/// </summary>
public class PlayerSetup : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private GameObject playerModel;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private CapsuleCollider capsuleCollider;

    [Header("Animation")]
    [SerializeField] private bool setupAnimationController = true;

    [Header("Skin Settings")]
    [SerializeField] private string defaultSkinId = "default_material";

    private SkinManager skinManager;
    private SkinManager.SkinData currentSkin;
    private PlayerAnimationController animationController;

    #region Unity Lifecycle

    private void Start()
    {
        Initialize();
        SetupPlayer();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// ������������� �����������
    /// </summary>
    private void Initialize()
    {
        if (playerModel == null)
        {
            playerModel = transform.Find("PlayerModel")?.gameObject;
        }

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (capsuleCollider == null)
        {
            capsuleCollider = GetComponent<CapsuleCollider>();
        }

        // ������������� ���������
        if (SkinManager.HasInstance)
        {
            skinManager = SkinManager.Instance;
        }

        // ������� ���������� �������� ���� �����
        if (setupAnimationController)
        {
            SetupAnimationController();
        }
    }

    /// <summary>
    /// ��������� ����������� ��������
    /// </summary>
    private void SetupAnimationController()
    {
        animationController = GetComponent<PlayerAnimationController>();
        if (animationController == null)
        {
            animationController = gameObject.AddComponent<PlayerAnimationController>();
        }
    }

    /// <summary>
    /// ��������� ������
    /// </summary>
    private void SetupPlayer()
    {
        ApplyDefaultSkin();
        SubscribeToEvents();
        Debug.Log("[PlayerSetup] Player initialized with default skin");
    }

    #endregion

    #region Skin Management

    /// <summary>
    /// ���������� ���������� �����
    /// </summary>
    private void ApplyDefaultSkin()
    {
        if (skinManager != null && !string.IsNullOrEmpty(defaultSkinId))
        {
            var skin = skinManager.GetSkin(defaultSkinId);
            if (skin != null)
            {
                currentSkin = skin;
                ApplySkin(skin);
            }
        }
    }

    /// <summary>
    /// ��������� ��������� �����
    /// </summary>
    private void OnSkinChanged(SkinManager.SkinData newSkin)
    {
        if (newSkin == null) return;

        currentSkin = newSkin;
        ApplySkin(newSkin);

        // ��������� �������� ��� ����� �����
        if (animationController != null)
        {
            animationController.OnSkinChanged(newSkin);
        }

        Debug.Log($"[PlayerSetup] Skin changed to {newSkin.skinName}");
    }

    /// <summary>
    /// ���������� �����
    /// </summary>
    private void ApplySkin(SkinManager.SkinData skin)
    {
        if (playerModel == null) return;

        switch (skin.skinType)
        {
            case SkinManager.SkinType.Material:
                ApplyMaterialSkin(skin);
                break;
            case SkinManager.SkinType.Model:
                ApplyModelSkin(skin);
                break;
        }
    }

    /// <summary>
    /// ���������� ������������� �����
    /// </summary>
    private void ApplyMaterialSkin(SkinManager.SkinData skin)
    {
        if (skin.material != null)
        {
            var renderer = playerModel.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = skin.material;
            }
        }
    }

    /// <summary>
    /// ���������� ���������� �����
    /// </summary>
    private void ApplyModelSkin(SkinManager.SkinData skin)
    {
        if (skin.modelPrefab == null) return;

        // ���������� ���������� ������
        Destroy(playerModel);

        // ������� ����� ������
        playerModel = Instantiate(skin.modelPrefab, transform.position, transform.rotation, transform);
        playerModel.name = "PlayerModel";

        // ��������� ������ � ������������ �����������
        if (animationController != null)
        {
            // ������������ ���������� ������� ������ �������������
        }
    }

    #endregion

    #region Event Handling

    /// <summary>
    /// �������� �� ������� ������
    /// </summary>
    private void SubscribeToEvents()
    {
        SkinManager.OnSkinEquipped += OnSkinChanged;
    }

    /// <summary>
    /// ������� �� ������� ������
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        SkinManager.OnSkinEquipped -= OnSkinChanged;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// ��������� �������� �����
    /// </summary>
    public SkinManager.SkinData GetCurrentSkin()
    {
        return currentSkin;
    }

    /// <summary>
    /// �������� �������� �� ����� ������������� ������
    /// </summary>
    public bool IsEquippedWithSkin(string skinId)
    {
        return currentSkin != null && currentSkin.skinId == skinId;
    }

    /// <summary>
    /// ��������� ����������� ��������
    /// </summary>
    public PlayerAnimationController GetAnimationController()
    {
        return animationController;
    }

    #endregion
}