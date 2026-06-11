// 参照用（Excel の Office Scripts として使用）: ブック内で完結する整形処理の例（記事「置換の最小サンプル」）
function main(workbook: ExcelScript.Workbook) {
  const sheet = workbook.getActiveWorksheet();
  const used = sheet.getUsedRange();
  used.getFormat().autofitColumns();

  const tables = workbook.getTables();
  if (tables.length > 0) {
    tables[0].getSort().apply([{ key: 0, ascending: true }], true);
  }
}
