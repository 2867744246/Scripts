using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 地图选择图标按钮，用于固定图标绑定对应地图索引。
/// </summary>
[RequireComponent(typeof(Button))]
public class MapIconButton : MonoBehaviour
{
    public int mapIndex;
    public MenuFlowManager flowManager;

    private Button button;

    private void Awake()
    {
        Debug.Log($"MapIconButton Awake, initial mapIndex: {mapIndex}, gameObject: {gameObject.name}");
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        Debug.Log($"MapIconButton OnClick, mapIndex: {mapIndex}, flowManager: {flowManager}");
        if (flowManager != null)
        {
            flowManager.OnMapIconClicked(mapIndex);
        }
        else
        {
            Debug.LogWarning("flowManager is null in MapIconButton.OnClick");
        }
    }
}
