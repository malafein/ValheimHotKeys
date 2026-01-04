using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace ValheimHotKeys
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGUID = "com.malafein.valheimhotkeys";
        public const string ModName = "Valheim HotKeys";
        public const string ModVersion = "1.0.0";

        public static ConfigEntry<KeyboardShortcut> ToggleHUDConfig;
        public static ConfigEntry<KeyboardShortcut>[] HotbarConfigs = new ConfigEntry<KeyboardShortcut>[8];
        
        public static System.Collections.Generic.List<ConfigEntry<ItemBinding>> CustomItemBindings = new System.Collections.Generic.List<ConfigEntry<ItemBinding>>();

        private readonly Harmony harmony = new Harmony(ModGUID);
        private static ConfigEntryBase _waitingEntry;
        private static int _activationFrame = -1;
        private static int _forbiddenFrame = -1;

        private void Awake()
        {
            TomlTypeConverter.AddConverter(typeof(ItemBinding), new TypeConverter
            {
                ConvertToObject = (str, type) => ItemBinding.Deserialize(str),
                ConvertToString = (obj, type) => ((ItemBinding)obj).Serialize()
            });

            ToggleHUDConfig = Config.Bind("General", "ToggleHUD", new KeyboardShortcut(KeyCode.F3), "Hotkey to toggle the HUD visibility.");
            
            for (int i = 0; i < 8; i++)
            {
                int slotNumber = i + 1;
                HotbarConfigs[i] = Config.Bind("Hotbar", $"Slot{slotNumber}", new KeyboardShortcut(KeyCode.None), $"Hotkey for hotbar slot {slotNumber}.");
            }

            for (int i = 0; i < 8; i++)
            {
                int slotNumber = i + 1;
                var attributes = new ConfigurationManagerAttributes { CustomDrawer = DrawItemBindingElement, HideDefaultButton = false };
                
                ItemBinding defaultBinding = new ItemBinding();
                if (i == 0)
                {
                    defaultBinding.ItemName = "Healing Mead";
                    defaultBinding.Shortcut = new KeyboardShortcut(KeyCode.Mouse3);
                }
                else if (i == 1)
                {
                    defaultBinding.ItemName = "Arrow";
                    defaultBinding.Shortcut = new KeyboardShortcut(KeyCode.Mouse4);
                }

                CustomItemBindings.Add(Config.Bind("Custom Item Bindings", $"Slot {slotNumber}", defaultBinding, new ConfigDescription($"Custom item binding for slot {slotNumber}.", null, attributes)));
            }

            Logger.LogInfo($"{ModName} {ModVersion} is loading...");
            harmony.PatchAll();
            Logger.LogInfo($"{ModName} loaded!");
        }

        private void DrawItemBindingElement(ConfigEntryBase entry)
        {
            var bindingEntry = (ConfigEntry<ItemBinding>)entry;
            ItemBinding binding = bindingEntry.Value;

            GUILayout.BeginHorizontal();
            
            // Item Name field
            string newName = GUILayout.TextField(binding.ItemName, GUILayout.Width(200));
            if (newName != binding.ItemName)
            {
                // IMMUTABLE UPDATE: Create a new object so BepInEx "Reset" recognizes the change
                bindingEntry.Value = new ItemBinding { ItemName = newName, Shortcut = binding.Shortcut };
            }

            GUILayout.Space(10);

            // Shortcut button
            bool isWaiting = _waitingEntry == entry;
            
            GUI.enabled = !isWaiting;
            string shortcutText = isWaiting ? "Press any key..." : binding.Shortcut.ToString();
            if (!isWaiting && (string.IsNullOrEmpty(shortcutText) || shortcutText == "None")) shortcutText = "Click to bind";

            if (GUILayout.Button(shortcutText, GUILayout.Width(150)))
            {
                _waitingEntry = entry;
                _activationFrame = Time.frameCount;
            }
            GUI.enabled = true;

            if (isWaiting)
            {
                if (GUILayout.Button("Cancel", GUILayout.Width(60)))
                {
                    _waitingEntry = null;
                    _forbiddenFrame = Time.frameCount; // Cooldown for Clear button
                    Event.current.Use();
                }
                else
                {
                    Event e = Event.current;
                    
                    if (Time.frameCount > _activationFrame && (e.type == EventType.KeyDown || e.type == EventType.MouseDown || e.type == EventType.Used))
                    {
                        if (e.type != EventType.Used && (e.type == EventType.MouseUp || e.type == EventType.MouseDrag || e.type == EventType.ScrollWheel))
                        {
                            // Ignore noise
                        }
                        else
                        {
                            KeyCode capturedKey = KeyCode.None;
                            if (e.keyCode != KeyCode.None) capturedKey = e.keyCode;
                            else if (e.button >= 0 && e.button <= 6) capturedKey = (KeyCode)((int)KeyCode.Mouse0 + e.button);

                            // IGNORE Mouse0 (Left Click) to prevent interference with UI buttons
                            if (capturedKey != KeyCode.None && capturedKey != KeyCode.Mouse0)
                            {
                                if (capturedKey == KeyCode.Escape)
                                {
                                    _waitingEntry = null;
                                    e.Use();
                                }
                                else if (capturedKey != KeyCode.LeftControl && capturedKey != KeyCode.RightControl &&
                                         capturedKey != KeyCode.LeftShift && capturedKey != KeyCode.RightShift &&
                                         capturedKey != KeyCode.LeftAlt && capturedKey != KeyCode.RightAlt &&
                                         capturedKey != KeyCode.LeftCommand && capturedKey != KeyCode.RightCommand)
                                {
                                    var modifiers = new System.Collections.Generic.List<KeyCode>();
                                    if (e.control) modifiers.Add(KeyCode.LeftControl);
                                    if (e.shift) modifiers.Add(KeyCode.LeftShift);
                                    if (e.alt) modifiers.Add(KeyCode.LeftAlt);

                                    // IMMUTABLE UPDATE
                                    bindingEntry.Value = new ItemBinding { ItemName = binding.ItemName, Shortcut = new KeyboardShortcut(capturedKey, modifiers.ToArray()) };
                                    _waitingEntry = null;
                                    e.Use();
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // Clear button with Forbidden Frame cooldown
                if (GUILayout.Button("Clear", GUILayout.Width(60)))
                {
                    if (Time.frameCount != _forbiddenFrame)
                    {
                        bindingEntry.Value = new ItemBinding { ItemName = "", Shortcut = KeyboardShortcut.Empty };
                        Event.current.Use();
                    }
                }
            }

            GUILayout.EndHorizontal();
        }

        public class ItemBinding
        {
            public string ItemName = "";
            public KeyboardShortcut Shortcut = KeyboardShortcut.Empty;

            public string Serialize()
            {
                if (string.IsNullOrEmpty(ItemName)) return "";
                return $"{ItemName}|{Shortcut.Serialize()}";
            }

            public static ItemBinding Deserialize(string str)
            {
                var result = new ItemBinding();
                if (string.IsNullOrEmpty(str)) return result;

                int separatorIndex = str.LastIndexOf('|');
                if (separatorIndex == -1)
                {
                    result.ItemName = str;
                    return result;
                }

                result.ItemName = str.Substring(0, separatorIndex);
                string shortcutStr = str.Substring(separatorIndex + 1);
                
                try
                {
                    result.Shortcut = KeyboardShortcut.Deserialize(shortcutStr);
                }
                catch
                {
                    result.Shortcut = KeyboardShortcut.Empty;
                }

                return result;
            }
        }

        // Helper class for Configuration Manager
        public class ConfigurationManagerAttributes
        {
            public System.Action<ConfigEntryBase> CustomDrawer;
            public bool? ShowMultilineText;
            public string DispName;
            public int? Order;
            public bool? HideDefaultButton;
            public bool? HideSettingName;
        }
    }
}
