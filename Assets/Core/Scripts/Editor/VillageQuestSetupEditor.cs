using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Editor window that auto-sets up the Village Dialogue & Quest system.
    /// Updated with sequential storyline matching Thánh Gióng legend.
    /// Access via menu: Thanh Giong > Setup Village Quest System
    /// </summary>
    public class VillageQuestSetupEditor : EditorWindow
    {
        private string panelSettingsPath = "Assets/Core/Settings/PanelSettings.asset";
        private bool setupDialogueSystem = true;
        private bool setupDialogueUI = true;
        private bool setupQuestManager = true;
        private bool createDialogueAssets = true;
        private bool addVillagerNPCComponents = true;

        [MenuItem("Thanh Giong/Setup Village Quest System")]
        public static void ShowWindow()
        {
            var window = GetWindow<VillageQuestSetupEditor>("Village Quest Setup");
            window.minSize = new Vector2(450, 550);
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Setup Hệ Thống Chat & Nhiệm Vụ Làng", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "CỐT TRUYỆN:\n" +
                "Step 1: Gặp Thanh Niên → biết giặc Ân sắp đến\n" +
                "Step 2: Gặp Già Làng → yêu cầu tâu vua\n" +
                "Step 3: Gặp Bà Lão → nhận cơm\n" +
                "Step 4: Gặp Bé Gái → nhận đồ ăn\n" +
                "Step 5: Gặp Bé Trai → nhận đồ ăn\n" +
                "→ Gióng biến hình → Chuyển sang Map1 đánh giặc!",
                MessageType.Info);

            GUILayout.Space(10);

            setupDialogueSystem = EditorGUILayout.Toggle("DialogueSystem", setupDialogueSystem);
            setupDialogueUI = EditorGUILayout.Toggle("DialogueUI (Khung chat)", setupDialogueUI);
            setupQuestManager = EditorGUILayout.Toggle("VillageQuestManager", setupQuestManager);
            createDialogueAssets = EditorGUILayout.Toggle("Tạo DialogueData (Nội dung chat)", createDialogueAssets);
            addVillagerNPCComponents = EditorGUILayout.Toggle("Thêm VillagerNPC vào NPCs", addVillagerNPCComponents);

            GUILayout.Space(5);
            panelSettingsPath = EditorGUILayout.TextField("Panel Settings Path", panelSettingsPath);

            GUILayout.Space(20);

            if (GUILayout.Button("SETUP TAT CA", GUILayout.Height(40)))
            {
                RunFullSetup();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Xóa Setup cũ (Reset)", GUILayout.Height(25)))
            {
                ResetSetup();
            }
        }

        private void ResetSetup()
        {
            // Delete old dialogue assets
            if (AssetDatabase.IsValidFolder("Assets/Core/DialogueData"))
            {
                AssetDatabase.DeleteAsset("Assets/Core/DialogueData");
            }

            // Remove old components and GameObjects
            var oldDialogueSys = Object.FindObjectOfType<DialogueSystem>();
            if (oldDialogueSys != null) Undo.DestroyObjectImmediate(oldDialogueSys.gameObject);

            var oldDialogueUI = Object.FindObjectOfType<DialogueUI>();
            if (oldDialogueUI != null) Undo.DestroyObjectImmediate(oldDialogueUI.gameObject);

            var oldQuestMgr = Object.FindObjectOfType<VillageQuestManager>();
            if (oldQuestMgr != null) Undo.DestroyObjectImmediate(oldQuestMgr.gameObject);

            // Remove VillagerNPC from all NPCs
            var villagerNPCs = Object.FindObjectsOfType<VillagerNPC>();
            foreach (var npc in villagerNPCs)
            {
                Undo.DestroyObjectImmediate(npc);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[Setup] Reset hoàn tất! Giờ chạy Setup lại.");
            EditorUtility.DisplayDialog("Reset!", "Đã xóa setup cũ. Giờ bấm SETUP TẤT CẢ lại.", "OK");
        }

        private void RunFullSetup()
        {
            string dialogueDataFolder = "Assets/Core/DialogueData";
            if (!AssetDatabase.IsValidFolder(dialogueDataFolder))
            {
                AssetDatabase.CreateFolder("Assets/Core", "DialogueData");
            }

            DialogueUI dialogueUIComponent = null;

            if (setupDialogueSystem) CreateDialogueSystem();
            if (setupDialogueUI) dialogueUIComponent = CreateDialogueUI();

            Dictionary<string, DialogueData> dialogueAssets = null;
            if (createDialogueAssets) dialogueAssets = CreateAllDialogueDataAssets(dialogueDataFolder);
            if (setupQuestManager) CreateQuestManager(dialogueUIComponent);
            if (addVillagerNPCComponents && dialogueAssets != null) AddVillagerNPCToExistingNPCs(dialogueAssets);

            EditorUtility.DisplayDialog("Setup hoàn tất!",
                "Cốt truyện mới đã được setup!\n\n" +
                "Flow: Thanh Niên → Già Làng → Bà Lão → Bé Gái → Bé Trai\n" +
                "→ Gióng biến hình → Map1 đánh giặc!\n\n" +
                "Nhấn Ctrl+S lưu scene rồi Play!",
                "OK");

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

        #region Setup Methods

        private void CreateDialogueSystem()
        {
            if (Object.FindObjectOfType<DialogueSystem>() != null)
            {
                Debug.Log("[Setup] DialogueSystem đã tồn tại.");
                return;
            }
            var go = new GameObject("--- DialogueSystem ---");
            go.AddComponent<DialogueSystem>();
            Undo.RegisterCreatedObjectUndo(go, "Create DialogueSystem");
            Debug.Log("[Setup] Đã tạo DialogueSystem");
        }

        private DialogueUI CreateDialogueUI()
        {
            var existing = Object.FindObjectOfType<DialogueUI>();
            if (existing != null) return existing;

            var go = new GameObject("--- DialogueUI ---");
            var uiDoc = go.AddComponent<UIDocument>();

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelSettingsPath);
            if (panelSettings != null) uiDoc.panelSettings = panelSettings;

            var dialogueUI = go.AddComponent<DialogueUI>();
            Undo.RegisterCreatedObjectUndo(go, "Create DialogueUI");
            return dialogueUI;
        }

        private void CreateQuestManager(DialogueUI dialogueUI)
        {
            if (Object.FindObjectOfType<VillageQuestManager>() != null)
            {
                Debug.Log("[Setup] VillageQuestManager đã tồn tại.");
                return;
            }

            var go = new GameObject("--- VillageQuestManager ---");
            var questManager = go.AddComponent<VillageQuestManager>();

            // Quest Tracker UI
            var questTrackerGO = new GameObject("QuestTrackerUI");
            questTrackerGO.transform.SetParent(go.transform);
            var questTrackerDoc = questTrackerGO.AddComponent<UIDocument>();
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelSettingsPath);
            if (panelSettings != null) questTrackerDoc.panelSettings = panelSettings;

            // Win Screen UI
            var winScreenGO = new GameObject("TransformationScreenUI");
            winScreenGO.transform.SetParent(go.transform);
            var winScreenDoc = winScreenGO.AddComponent<UIDocument>();
            if (panelSettings != null) winScreenDoc.panelSettings = panelSettings;

            var so = new SerializedObject(questManager);
            so.FindProperty("questTrackerUIDocument").objectReferenceValue = questTrackerDoc;
            so.FindProperty("winScreenUIDocument").objectReferenceValue = winScreenDoc;
            if (dialogueUI != null) so.FindProperty("dialogueUI").objectReferenceValue = dialogueUI;
            so.FindProperty("battleSceneName").stringValue = "Map1";
            so.FindProperty("transformationDuration").floatValue = 4f;
            so.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(go, "Create VillageQuestManager");
        }

        /// <summary>
        /// Creates all DialogueData assets with the NEW storyline.
        /// Quest order: Thanh Niên(0) → Già Làng(1) → Bà Lão(2) → Bé Gái(3) → Bé Trai(4)
        /// </summary>
        private Dictionary<string, DialogueData> CreateAllDialogueDataAssets(string folder)
        {
            var assets = new Dictionary<string, DialogueData>();

            // ===== STEP 0: THANH NIÊN - Kể về giặc Ân =====
            assets["Thanhnien1"] = CreateDialogueAsset(folder, "Dialogue_ThanhNien", "Thanh Niên", new DialogueLine[]
            {
                new DialogueLine { speakerName = "Thanh Niên", text = "Này, cậu là ai? Trông cậu có vẻ không phải người làng này...", isPlayerLine = false },
                new DialogueLine { speakerName = "Gióng", text = "Tôi là Gióng. Tôi nghe tin có chuyện gì đó xảy ra ở làng?", isPlayerLine = true },
                new DialogueLine { speakerName = "Thanh Niên", text = "Cậu chưa biết sao? Giặc Ân đang kéo đại quân từ phương Bắc xuống!", isPlayerLine = false },
                new DialogueLine { speakerName = "Thanh Niên", text = "Quân chúng đông lắm, binh mã hàng vạn... Nước ta chẳng có người tài nào đủ sức chống đỡ.", isPlayerLine = false },
                new DialogueLine { speakerName = "Thanh Niên", text = "Vua đã sai sứ đi khắp nơi tìm người hiền tài cứu nước, nhưng vẫn chưa ai đứng ra cả.", isPlayerLine = false },
                new DialogueLine { speakerName = "Thanh Niên", text = "E rằng đợt này... khó mà giữ được nước rồi.", isPlayerLine = false },
                new DialogueLine { speakerName = "Gióng", text = "Giặc Ân hả... Anh cho tôi hỏi, già làng ở đâu? Tôi muốn gặp ông ấy!", isPlayerLine = true },
                new DialogueLine { speakerName = "Thanh Niên", text = "Già làng hả? Ông ấy đang ở phía bên kia làng. Cậu đi thẳng rồi rẽ là thấy.", isPlayerLine = false },
                new DialogueLine { speakerName = "Thanh Niên", text = "Nhưng mà... cậu bé nhỏ thế này định làm gì? Đánh giặc không phải chuyện đùa đâu!", isPlayerLine = false },
                new DialogueLine { speakerName = "Gióng", text = "Anh cứ yên tâm. Tôi sẽ tìm cách cứu nước!", isPlayerLine = true },
            });

            // ===== STEP 1: GIÀ LÀNG - Yêu cầu tâu vua =====
            assets["tripo_convert_102a4e0d-7921-4345-9060-014354977cf0"] = CreateDialogueAsset(folder, "Dialogue_GiaLang", "Già Làng", new DialogueLine[]
            {
                new DialogueLine { speakerName = "Già Làng", text = "Ồ, có ai đến tìm ta? Con là ai vậy?", isPlayerLine = false },
                new DialogueLine { speakerName = "Gióng", text = "Thưa ông, con là Gióng. Con nghe tin giặc Ân sắp đến.", isPlayerLine = true },
                new DialogueLine { speakerName = "Già Làng", text = "Đúng vậy con ạ. Giặc Ân hung tàn lắm, cả nước đang lo lắng.", isPlayerLine = false },
                new DialogueLine { speakerName = "Gióng", text = "Ông ơi, xin ông hãy tâu với nhà vua rằng con sẽ đánh giặc!", isPlayerLine = true },
                new DialogueLine { speakerName = "Già Làng", text = "Con... con nói thật đó sao? Con còn nhỏ thế này...", isPlayerLine = false },
                new DialogueLine { speakerName = "Gióng", text = "Con nói thật! Nhưng con cần nhà vua chuẩn bị cho con: một con ngựa sắt, một cây roi sắt, và một bộ áo giáp sắt!", isPlayerLine = true },
                new DialogueLine { speakerName = "Già Làng", text = "Ngựa sắt? Roi sắt? Áo giáp sắt? Con bé nhỏ thế này mà đòi những thứ đó?", isPlayerLine = false },
                new DialogueLine { speakerName = "Gióng", text = "Xin ông hãy tin con! Hãy tâu với vua và bảo dân làng mang cơm gạo đến cho con. Con cần ăn thật nhiều để có sức đánh giặc!", isPlayerLine = true },
                new DialogueLine { speakerName = "Già Làng", text = "Ta... ta thấy trong mắt con có một ngọn lửa khác thường. Được rồi, ta sẽ tâu với vua ngay!", isPlayerLine = false },
                new DialogueLine { speakerName = "Già Làng", text = "Còn con, hãy đi gặp bà con trong làng. Mọi người sẽ mang cơm gạo đến cho con!", isPlayerLine = false },
                new DialogueLine { speakerName = "Gióng", text = "Con cảm ơn ông! Con sẽ không phụ lòng dân làng!", isPlayerLine = true },
            });

            // ===== STEP 2: BÀ LÃO - Mang cơm đến =====
            assets["Balao"] = CreateDialogueAsset(folder, "Dialogue_BaLao", "Bà Lão", new DialogueLine[]
            {
                new DialogueLine { speakerName = "Bà Lão", text = "Con ơi! Ta nghe già làng nói rồi. Con là cậu bé muốn đánh giặc phải không?", isPlayerLine = false },
                new DialogueLine { speakerName = "Gióng", text = "Dạ thưa bà, đúng ạ. Con cần ăn thật nhiều để có sức chiến đấu!", isPlayerLine = true },
                new DialogueLine { speakerName = "Bà Lão", text = "Tội nghiệp con! Ta nấu sẵn nồi cơm nếp đây rồi. Con ăn đi cho chóng lớn!", isPlayerLine = false },
                new DialogueLine { speakerName = "Bà Lão", text = "Ta góp hết mấy gánh gạo nhà ta. Có bao nhiêu đem cho con hết!", isPlayerLine = false },
                new DialogueLine { speakerName = "Gióng", text = "Con cảm ơn bà! Cơm nếp của bà thơm quá!", isPlayerLine = true },
                new DialogueLine { speakerName = "Bà Lão", text = "Ăn đi con, ăn cho no vào! Mấy đứa nhỏ trong xóm cũng đang mang đồ ăn đến cho con đó!", isPlayerLine = false },
            });

            // ===== STEP 3: BÉ GÁI - Mang đồ ăn =====
            assets["begai"] = CreateDialogueAsset(folder, "Dialogue_BeGai", "Bé Gái", new DialogueLine[]
            {
                new DialogueLine { speakerName = "Bé Gái", text = "Anh Gióng ơi! Em mang khoai lang và bắp ngô đến cho anh nè!", isPlayerLine = false },
                new DialogueLine { speakerName = "Gióng", text = "Ồ, cảm ơn em! Em mang nhiều quá!", isPlayerLine = true },
                new DialogueLine { speakerName = "Bé Gái", text = "Mẹ em bảo phải mang thật nhiều cho anh ăn. Anh ăn bao nhiêu cũng không đủ phải không?", isPlayerLine = false },
                new DialogueLine { speakerName = "Gióng", text = "Đúng rồi! Tôi cần ăn thật nhiều để lớn nhanh đánh giặc!", isPlayerLine = true },
                new DialogueLine { speakerName = "Bé Gái", text = "Em sợ giặc lắm... Anh đánh thắng giặc rồi quay về nha!", isPlayerLine = false },
                new DialogueLine { speakerName = "Gióng", text = "Em yên tâm! Tôi hứa sẽ đuổi hết giặc Ân ra khỏi nước ta!", isPlayerLine = true },
            });

            // ===== STEP 4: BÉ TRAI - Mang đồ ăn (cuối cùng) =====
            assets["betrai"] = CreateDialogueAsset(folder, "Dialogue_BeTrai", "Bé Trai", new DialogueLine[]
            {
                new DialogueLine { speakerName = "Bé Trai", text = "Anh Gióng! Anh Gióng! Em cũng mang cơm đến cho anh nè!", isPlayerLine = false },
                new DialogueLine { speakerName = "Gióng", text = "Cảm ơn em! Mọi người trong làng tốt quá!", isPlayerLine = true },
                new DialogueLine { speakerName = "Bé Trai", text = "Ba em nói anh ăn bao nhiêu cũng hết, cả làng gom cơm gạo lại cho anh luôn đó!", isPlayerLine = false },
                new DialogueLine { speakerName = "Gióng", text = "Tôi sẽ không bao giờ quên tấm lòng của dân làng!", isPlayerLine = true },
                new DialogueLine { speakerName = "Bé Trai", text = "Em nghe nói vua đã cho thợ rèn làm ngựa sắt và roi sắt cho anh rồi! Sắp đem tới rồi!", isPlayerLine = false },
                new DialogueLine { speakerName = "Gióng", text = "Thật sao?! Vậy thì... tôi đã sẵn sàng!", isPlayerLine = true },
                new DialogueLine { speakerName = "Bé Trai", text = "Kìa! Có đoàn người của nhà vua đang đi tới! Họ mang theo ngựa sắt và áo giáp kìa!", isPlayerLine = false },
                new DialogueLine { speakerName = "Gióng", text = "Đã đến lúc rồi... Tôi cảm thấy sức mạnh đang dâng trào trong người!", isPlayerLine = true },
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Setup] Đã tạo {assets.Count} DialogueData assets");
            return assets;
        }

        private DialogueData CreateDialogueAsset(string folder, string fileName, string npcName, DialogueLine[] lines)
        {
            string assetPath = $"{folder}/{fileName}.asset";

            // Delete old version if exists to update content
            var existing = AssetDatabase.LoadAssetAtPath<DialogueData>(assetPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            var data = ScriptableObject.CreateInstance<DialogueData>();
            data.npcName = npcName;
            data.playOnce = true;
            data.lines = new List<DialogueLine>(lines);

            AssetDatabase.CreateAsset(data, assetPath);
            return data;
        }

        /// <summary>
        /// Maps NPC GameObjects to their quest step and dialogue.
        /// Quest order: Thanhnien1(0) → tripo/GiaLang(1) → Balao(2) → begai(3) → betrai(4)
        /// </summary>
        private void AddVillagerNPCToExistingNPCs(Dictionary<string, DialogueData> dialogueAssets)
        {
            var villagerAIs = Object.FindObjectsOfType<VillagerAI>();

            if (villagerAIs.Length == 0)
            {
                Debug.LogWarning("[Setup] Không tìm thấy VillagerAI nào trong scene!");
                return;
            }

            // Define quest step mapping: GameObject name → quest step index
            var stepMapping = new Dictionary<string, int>
            {
                { "Thanhnien1", 0 },
                { "tripo_convert_102a4e0d-7921-4345-9060-014354977cf0", 1 },
                { "Balao", 2 },
                { "begai", 3 },
                { "betrai", 4 },
            };

            // Collect added VillagerNPCs for quest manager
            var addedVillagerNPCs = new List<VillagerNPC>();

            foreach (var villagerAI in villagerAIs)
            {
                // Remove old VillagerNPC if exists
                var oldNPC = villagerAI.GetComponent<VillagerNPC>();
                if (oldNPC != null)
                {
                    Undo.DestroyObjectImmediate(oldNPC);
                }

                var villagerNPC = Undo.AddComponent<VillagerNPC>(villagerAI.gameObject);
                string goName = villagerAI.gameObject.name;

                // Find matching dialogue
                DialogueData matchedDialogue = null;
                int matchedStep = -1;

                foreach (var kvp in dialogueAssets)
                {
                    if (goName.Contains(kvp.Key) || kvp.Key.Contains(goName))
                    {
                        matchedDialogue = kvp.Value;
                        break;
                    }
                }

                // Find quest step
                foreach (var kvp in stepMapping)
                {
                    if (goName.Contains(kvp.Key) || kvp.Key.Contains(goName))
                    {
                        matchedStep = kvp.Value;
                        break;
                    }
                }

                var so = new SerializedObject(villagerNPC);

                if (matchedDialogue != null)
                {
                    so.FindProperty("villagerName").stringValue = matchedDialogue.npcName;
                    var diagArray = so.FindProperty("dialogueDatas");
                    if (diagArray != null)
                    {
                        diagArray.arraySize = 1;
                        diagArray.GetArrayElementAtIndex(0).objectReferenceValue = matchedDialogue;
                    }
                }
                else
                {
                    so.FindProperty("villagerName").stringValue = goName;
                }

                var stepArray = so.FindProperty("questSteps");
                if (stepArray != null)
                {
                    stepArray.arraySize = 1;
                    stepArray.GetArrayElementAtIndex(0).intValue = matchedStep >= 0 ? matchedStep : 99;
                }

                var radiusProp = so.FindProperty("interactionRadius");
                if (radiusProp != null) radiusProp.floatValue = 5f;

                so.ApplyModifiedProperties();
                addedVillagerNPCs.Add(villagerNPC);

                Debug.Log($"[Setup] '{goName}' → Step {matchedStep}, Dialogue: {(matchedDialogue != null ? matchedDialogue.npcName : "NONE")}");
            }

            // Sort by quest step and assign to quest manager
            addedVillagerNPCs.Sort((a, b) => 
            {
                int stepA = (a.QuestSteps != null && a.QuestSteps.Length > 0) ? a.QuestSteps[0] : 0;
                int stepB = (b.QuestSteps != null && b.QuestSteps.Length > 0) ? b.QuestSteps[0] : 0;
                return stepA.CompareTo(stepB);
            });

            var questManager = Object.FindObjectOfType<VillageQuestManager>();
            if (questManager != null)
            {
                var qmSO = new SerializedObject(questManager);
                var villagersProp = qmSO.FindProperty("allVillagers");
                villagersProp.ClearArray();
                for (int i = 0; i < addedVillagerNPCs.Count; i++)
                {
                    villagersProp.InsertArrayElementAtIndex(i);
                    villagersProp.GetArrayElementAtIndex(i).objectReferenceValue = addedVillagerNPCs[i];
                }
                qmSO.ApplyModifiedProperties();
                Debug.Log($"[Setup] Đã gán {addedVillagerNPCs.Count} NPCs vào QuestManager theo thứ tự quest");
            }
        }

        #endregion
    }
}
