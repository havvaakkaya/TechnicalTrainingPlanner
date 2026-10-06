using ClosedXML.Excel;
using TechnicalTrainingPlanner.ViewModels.Planning;

namespace TechnicalTrainingPlanner.Infrastructure.Export;

public sealed class ExcelExportService
{
    public byte[] Create(ReportPage report)
    {
        using var workbook = new XLWorkbook();
        var assigned = workbook.Worksheets.Add("Atamalar");
        Heading(assigned, "ATAMALAR · KURGUSAL PİLOT VERİ", new[]
        { "Çalışan", "Birim", "Eğitim", "Son tarih", "Oturum (İstanbul)", "Sınıf", "Eğitmen", "Öncelik", "Kritik" });
        int row = 4;
        foreach (var item in report.Assignments)
        {
            assigned.Cell(row, 1).Value = item.Employee;
            assigned.Cell(row, 2).Value = item.Department;
            assigned.Cell(row, 3).Value = item.Training;
            if (item.DueOn.HasValue) assigned.Cell(row, 4).Value = item.DueOn.Value.ToDateTime(TimeOnly.MinValue);
            assigned.Cell(row, 5).Value = item.StartsLocal;
            assigned.Cell(row, 6).Value = item.Classroom;
            assigned.Cell(row, 7).Value = item.Instructor;
            assigned.Cell(row, 8).Value = item.Priority;
            assigned.Cell(row, 9).Value = item.Critical ? "Evet" : "Hayır";
            row++;
        }
        assigned.Column(4).Style.DateFormat.Format = "dd.mm.yyyy";
        assigned.Column(5).Style.DateFormat.Format = "dd.mm.yyyy hh:mm";
        Finish(assigned, row - 1, 9);

        var unassigned = workbook.Worksheets.Add("Atanamayanlar");
        Heading(unassigned, "ATANAMAYANLAR · KURGUSAL PİLOT VERİ", new[]
        { "Çalışan", "Birim", "Eğitim", "Son tarih", "Öncelik", "Kritik", "Doğrulanabilir nedenler" });
        row = 4;
        foreach (var item in report.Unassigned)
        {
            unassigned.Cell(row, 1).Value = item.Employee;
            unassigned.Cell(row, 2).Value = item.Department;
            unassigned.Cell(row, 3).Value = item.Training;
            if (item.DueOn.HasValue) unassigned.Cell(row, 4).Value = item.DueOn.Value.ToDateTime(TimeOnly.MinValue);
            unassigned.Cell(row, 5).Value = item.Priority;
            unassigned.Cell(row, 6).Value = item.Critical ? "Evet" : "Hayır";
            unassigned.Cell(row, 7).Value = item.Reasons;
            row++;
        }
        unassigned.Column(4).Style.DateFormat.Format = "dd.mm.yyyy";
        Finish(unassigned, row - 1, 7);
        unassigned.Column(7).Width = 65;

        var summary = workbook.Worksheets.Add("Rapor özeti");
        summary.Cell("A1").Value = "Teknik eğitim planı · Kurgusal pilot veri";
        summary.Cell("A1").Style.Font.Bold = true;
        summary.Cell("A1").Style.Font.FontSize = 15;
        string[] labels = { "Analiz", "Planlama ayı", "Yöntem", "Filtredeki ihtiyaçlar",
            "Filtrede atanan", "Filtrede atanamayan", "Bütün analizde ihtiyaç", "Birim filtresi",
            "Eğitim filtresi", "Durum filtresi", "Analiz güncel mi?" };
        string[] values = { report.AnalysisId.ToString(), report.Start.ToString("MM.yyyy"), report.Method,
            report.Needs.ToString(), report.Assignments.Count.ToString(), report.Unassigned.Count.ToString(),
            report.TotalNeeds.ToString(), report.Departments.FirstOrDefault(x => x.Id == report.DepartmentId)?.Name ?? "Tümü",
            report.Trainings.FirstOrDefault(x => x.Id == report.TrainingId).Name ?? "Tümü",
            report.Status ?? "Tümü", report.IsCurrent ? "Evet" : "Hayır" };
        for (int i = 0; i < labels.Length; i++)
        {
            summary.Cell(i + 3, 1).Value = labels[i];
            summary.Cell(i + 3, 2).Value = values[i];
        }
        summary.Cell("A16").Value = "Bu dosya gerçek kişi veya şirket performans verisi içermez.";
        for (int column = 1; column <= 2; column++)
        {
            summary.Column(column).AdjustToContents();
            summary.Column(column).Width = Math.Min(summary.Column(column).Width, 55);
        }

        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
    }

    private static void Heading(IXLWorksheet sheet, string title, string[] headers)
    {
        sheet.Cell(1, 1).Value = title;
        sheet.Range(1, 1, 1, headers.Length).Merge();
        sheet.Range(1, 1, 1, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#17324D");
        sheet.Range(1, 1, 1, headers.Length).Style.Font.FontColor = XLColor.White;
        sheet.Range(1, 1, 1, headers.Length).Style.Font.Bold = true;
        sheet.Cell(2, 1).Value = "Yalnız ekrandaki seçili filtreleri kapsar.";
        for (int i = 0; i < headers.Length; i++) sheet.Cell(3, i + 1).Value = headers[i];
        var header = sheet.Range(3, 1, 3, headers.Length);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#E6F0F8");
        sheet.SheetView.FreezeRows(3);
    }

    private static void Finish(IXLWorksheet sheet, int lastRow, int columns)
    {
        for (int column = 1; column <= columns; column++)
        {
            sheet.Column(column).AdjustToContents();
            sheet.Column(column).Width = Math.Min(sheet.Column(column).Width, 48);
        }
        if (lastRow >= 4) sheet.Range(3, 1, lastRow, columns).SetAutoFilter();
    }
}
