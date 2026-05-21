using MushOut.Player;
using UnityEngine;
using UnityEngine.UI;

namespace MushOut.UI
{
    public class PlayerAbilityIconBinder : MonoBehaviour
    {
        [SerializeField] private AbilityController abilityController;
        [SerializeField] private Image weaponIcon;
        [SerializeField] private Text keyText;
        [SerializeField] private Text nameText;
        [SerializeField] private Text countText;

        private Sprite _dashIcon;
        private Sprite _sleepSporeIcon;
        private Sprite _provocationIcon;
        private Sprite _bombSporeIcon;

        private void Awake()
        {
            LoadIcons();
            ResolveReferences();
        }

        private void LateUpdate()
        {
            ResolveReferences();
            if (abilityController == null || weaponIcon == null) return;

            AbilityState state = abilityController.CurrentState;

            Sprite icon = GetIcon(state);
            weaponIcon.sprite = icon;
            weaponIcon.overrideSprite = icon;

            if (keyText != null)
            {
                keyText.text = GetKeyLabel(state);
            }

            if (nameText != null)
            {
                nameText.text = GetDisplayName(state);
            }

            if (countText != null)
            {
                countText.text = $"x{abilityController.GetResourceCount(state)}";
            }

        }

        private void ResolveReferences()
        {
            if (abilityController == null)
            {
                abilityController = FindFirstObjectByType<AbilityController>();
            }

            if (weaponIcon == null)
            {
                weaponIcon = FindImage("WeaponIcon");
            }

            if (keyText == null)
            {
                keyText = FindText("KeyLabel");
            }

            if (nameText == null)
            {
                nameText = FindText("AbilityName");
            }

            if (countText == null)
            {
                countText = FindText("Count");
            }
        }

        private Image FindImage(string objectName)
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i].name == objectName)
                {
                    return images[i];
                }
            }

            return null;
        }

        private Text FindText(string objectName)
        {
            Text[] texts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].name == objectName)
                {
                    return texts[i];
                }
            }

            return null;
        }

        private void LoadIcons()
        {
            _dashIcon = Resources.Load<Sprite>("AbilityIcons/dash");
            _sleepSporeIcon = Resources.Load<Sprite>("AbilityIcons/sleep_spore");
            _provocationIcon = Resources.Load<Sprite>("AbilityIcons/Provocation");
            _bombSporeIcon = Resources.Load<Sprite>("AbilityIcons/boom");
        }

        private Sprite GetIcon(AbilityState state)
        {
            switch (state)
            {
                case AbilityState.Paralyze:
                    return _sleepSporeIcon;
                case AbilityState.Mad:
                    return _provocationIcon;
                case AbilityState.Bomb:
                    return _bombSporeIcon;
                default:
                    return _dashIcon;
            }
        }

        private string GetKeyLabel(AbilityState state)
        {
            switch (state)
            {
                case AbilityState.Paralyze:
                    return "2";
                case AbilityState.Mad:
                    return "3";
                case AbilityState.Bomb:
                    return "4";
                default:
                    return "1";
            }
        }

        private string GetDisplayName(AbilityState state)
        {
            switch (state)
            {
                case AbilityState.Paralyze:
                    return "SLEEP SPORE";
                case AbilityState.Mad:
                    return "TAUNT SPORE";
                case AbilityState.Bomb:
                    return "BOMB SPORE";
                default:
                    return "DASH";
            }
        }
    }
}
