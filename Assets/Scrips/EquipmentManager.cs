using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public enum ToolType
    {
        FlySwatter,
        RepairTape
    }

    public static EquipmentManager Instance { get; private set; }

    [Header("Equipment Settings")]
    [SerializeField]
    private ToolType startingTool =
        ToolType.FlySwatter;

    [Header("Repair Settings")]
    [Min(1)]
    public int tapeRepairAmount = 1;

    public static ToolType SelectedTool { get; private set; } =
        ToolType.FlySwatter;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SelectedTool = startingTool;
    }

    private void Update()
    {
        if (BreakableGlass.InputBlocked)
            return;

        // ปุ่ม 1 = ไม้ตียุง
        if (Input.GetKeyDown(KeyCode.Alpha1) ||
            Input.GetKeyDown(KeyCode.Keypad1))
        {
            SelectTool(ToolType.FlySwatter);
        }

        // ปุ่ม 2 = เทปซ่อมกระจก
        if (Input.GetKeyDown(KeyCode.Alpha2) ||
            Input.GetKeyDown(KeyCode.Keypad2))
        {
            SelectTool(ToolType.RepairTape);
        }
    }

    public void SelectTool(ToolType tool)
    {
        SelectedTool = tool;

        if (tool == ToolType.FlySwatter)
        {
            Debug.Log("เลือกอุปกรณ์: ไม้ตียุง");
        }
        else
        {
            Debug.Log("เลือกอุปกรณ์: เทปซ่อมกระจก");
        }
    }

    public static int GetRepairAmount()
    {
        if (Instance == null)
            return 1;

        return Mathf.Max(1, Instance.tapeRepairAmount);
    }
}