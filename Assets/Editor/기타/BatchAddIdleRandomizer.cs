using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 폴더 안의 모든 Animator Controller를 찾아서, "Idle" 이름을 포함한 State에
/// RandomizeIdleStart StateMachineBehaviour를 일괄로 추가해주는 툴.
/// 이미 붙어있으면 중복으로 추가하지 않는다.
/// </summary>
public class BatchAddIdleRandomizer : EditorWindow
{
    string _searchFolder = "Assets";
    string _stateNameFilter = "Idle"; // 이 문자열을 포함하는 State만 대상으로 함
    bool _dryRun = true;

    List<string> _log = new List<string>();
    Vector2 _scroll;

    [MenuItem("Tools/Animation/Batch Add Idle Randomizer")]
    public static void ShowWindow()
    {
        var win = GetWindow<BatchAddIdleRandomizer>("Idle Randomizer 일괄 추가");
        win.minSize = new Vector2(440, 380);
    }

    void OnGUI()
    {
        GUILayout.Space(10);

        GUILayout.Label("Idle 상태에 RandomizeIdleStart 일괄 추가", EditorStyles.boldLabel);

        _searchFolder = EditorGUILayout.TextField("검색 폴더", _searchFolder);
        _stateNameFilter = EditorGUILayout.TextField("State 이름 필터(포함 문자열)", _stateNameFilter);

        GUILayout.Space(5);

        _dryRun = EditorGUILayout.ToggleLeft(
            "Dry Run (실제로 저장하지 않고 무엇이 처리될지만 확인)",
            _dryRun
        );

        GUILayout.Space(10);

        if (GUILayout.Button(_dryRun ? "미리보기 실행" : "실제 실행 (저장됨)", GUILayout.Height(30)))
        {
            EditorApplication.delayCall += Run;
        }

        GUILayout.Space(10);
        GUILayout.Label("로그", EditorStyles.boldLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));

        foreach (var line in _log)
        {
            EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);
        }

        EditorGUILayout.EndScrollView();
    }

    void Run()
    {
        _log.Clear();

        if (!AssetDatabase.IsValidFolder(_searchFolder))
        {
            _log.Add($"[오류] 폴더가 없습니다: {_searchFolder}");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:AnimatorController", new[] { _searchFolder });

        if (guids.Length == 0)
        {
            _log.Add($"[오류] {_searchFolder} 안에 Animator Controller가 없습니다.");
            return;
        }

        int addedCount = 0, alreadyCount = 0, notFoundCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);

            if (controller == null) continue;

            var matchedStates = FindMatchingStates(controller, _stateNameFilter);

            if (matchedStates.Count == 0)
            {
                _log.Add($"[건너뜀] '{_stateNameFilter}' 포함 State 없음: {path}");
                notFoundCount++;
                continue;
            }

            foreach (var state in matchedStates)
            {
                bool alreadyHas = state.behaviours.Any(b => b is RandomizeIdleStart);

                if (alreadyHas)
                {
                    _log.Add($"[이미 있음] {path} → {state.name}");
                    alreadyCount++;
                    continue;
                }

                if (!_dryRun)
                {
                    state.AddStateMachineBehaviour<RandomizeIdleStart>();
                    EditorUtility.SetDirty(controller);
                }

                _log.Add($"[{(_dryRun ? "미리보기" : "추가됨")}] {path} → {state.name}");
                addedCount++;
            }
        }

        if (!_dryRun)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        string summary =
            $"추가 {addedCount}개 / 이미 있음 {alreadyCount}개 / 대상 없음 {notFoundCount}개 " +
            $"(검사한 Controller {guids.Length}개)";

        _log.Add("");
        _log.Add(summary);

        EditorUtility.DisplayDialog(
            _dryRun ? "미리보기 완료" : "일괄 추가 완료",
            summary + (_dryRun ? "\n\n(Dry Run이라 실제로 저장되지 않았습니다.)" : ""),
            "확인"
        );

        Repaint();
    }

    // 컨트롤러의 모든 레이어를 뒤져서 이름에 filter가 포함된 State를 전부 찾는다
    static List<AnimatorState> FindMatchingStates(AnimatorController controller, string filter)
    {
        var result = new List<AnimatorState>();

        foreach (var layer in controller.layers)
        {
            CollectStates(layer.stateMachine, filter, result);
        }

        return result;
    }

    static void CollectStates(AnimatorStateMachine stateMachine, string filter, List<AnimatorState> result)
    {
        foreach (var childState in stateMachine.states)
        {
            if (childState.state.name.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result.Add(childState.state);
            }
        }

        // 서브 스테이트 머신 안에도 Idle이 있을 수 있으니 재귀 탐색
        foreach (var childMachine in stateMachine.stateMachines)
        {
            CollectStates(childMachine.stateMachine, filter, result);
        }
    }
}
