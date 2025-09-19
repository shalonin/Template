using UnityEngine;

/// <summary>
/// Скрипт для настройки игрока при старте с поддержкой анимаций
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
    /// Инициализация компонентов
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

        // Инициализация синглтона
        if (SkinManager.HasInstance)
        {
            skinManager = SkinManager.Instance;
        }

        // Создаем контроллер анимаций если нужно
        if (setupAnimationController)
        {
            SetupAnimationController();
        }
    }

    /// <summary>
    /// Настройка контроллера анимаций
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
    /// Настройка игрока
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
    /// Применение дефолтного скина
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
    /// Обработка изменения скина
    /// </summary>
    private void OnSkinChanged(SkinManager.SkinData newSkin)
    {
        if (newSkin == null) return;

        currentSkin = newSkin;
        ApplySkin(newSkin);

        // Обновляем анимации при смене скина
        if (animationController != null)
        {
            animationController.OnSkinChanged(newSkin);
        }

        Debug.Log($"[PlayerSetup] Skin changed to {newSkin.skinName}");
    }

    /// <summary>
    /// Применение скина
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
    /// Применение материального скина
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
    /// Применение модельного скина
    /// </summary>
    private void ApplyModelSkin(SkinManager.SkinData skin)
    {
        if (skin.modelPrefab == null) return;

        // Уничтожаем предыдущую модель
        Destroy(playerModel);

        // Создаем новую модель
        playerModel = Instantiate(skin.modelPrefab, transform.position, transform.rotation, transform);
        playerModel.name = "PlayerModel";

        // Обновляем ссылку в анимационном контроллере
        if (animationController != null)
        {
            // Анимационный контроллер обновит ссылку автоматически
        }
    }

    #endregion

    #region Event Handling

    /// <summary>
    /// Подписка на события скинов
    /// </summary>
    private void SubscribeToEvents()
    {
        SkinManager.OnSkinEquipped += OnSkinChanged;
    }

    /// <summary>
    /// Отписка от событий скинов
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        SkinManager.OnSkinEquipped -= OnSkinChanged;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Получение текущего скина
    /// </summary>
    public SkinManager.SkinData GetCurrentSkin()
    {
        return currentSkin;
    }

    /// <summary>
    /// Проверка является ли игрок экипированным скином
    /// </summary>
    public bool IsEquippedWithSkin(string skinId)
    {
        return currentSkin != null && currentSkin.skinId == skinId;
    }

    /// <summary>
    /// Получение контроллера анимаций
    /// </summary>
    public PlayerAnimationController GetAnimationController()
    {
        return animationController;
    }

    #endregion
}