using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace OpenerCreator;

public static class Sheets
{
    public static readonly ExcelSheet<Action> ActionSheet;

    static Sheets()
    {
        ActionSheet = Plugin.DataManager.GetExcelSheet<Action>();
    }
}