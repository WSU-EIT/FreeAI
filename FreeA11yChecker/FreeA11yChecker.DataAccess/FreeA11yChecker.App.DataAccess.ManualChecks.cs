namespace FreeA11yChecker;

public partial interface IDataAccess
{
    Task<List<DataObjects.ManualCheckResult>> GetManualChecks(List<Guid>? Ids, Guid SiteId);
    Task<List<DataObjects.ManualCheckResult>> SaveManualChecks(List<DataObjects.ManualCheckResult> Items, DataObjects.User? CurrentUser = null);
}

public partial class DataAccess
{
    public async Task<List<DataObjects.ManualCheckResult>> GetManualChecks(List<Guid>? Ids, Guid SiteId)
    {
        List<DataObjects.ManualCheckResult> output = new List<DataObjects.ManualCheckResult>();

        try {
            IQueryable<EFModels.EFModels.ManualCheckResult> query = data.ManualCheckResults.Where(x => x.SiteId == SiteId);

            if (Ids != null && Ids.Any()) {
                query = query.Where(x => Ids.Contains(x.ManualCheckResultId));
            }

            query = query.OrderBy(x => x.WcagCriterion);

            List<EFModels.EFModels.ManualCheckResult> recs = await query.ToListAsync();

            if (recs != null && recs.Any()) {
                foreach (EFModels.EFModels.ManualCheckResult rec in recs) {
                    output.Add(new DataObjects.ManualCheckResult {
                        ActionResponse = GetNewActionResponse(true),
                        ManualCheckResultId = rec.ManualCheckResultId,
                        SiteId = rec.SiteId,
                        WcagCriterion = rec.WcagCriterion,
                        Status = rec.Status,
                        Notes = rec.Notes,
                        TestedBy = rec.TestedBy,
                        TestedAt = rec.TestedAt,
                    });
                }
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetManualChecks: " + RecurseExceptionAsString(ex));
        }

        return output;
    }

    public async Task<List<DataObjects.ManualCheckResult>> SaveManualChecks(List<DataObjects.ManualCheckResult> Items, DataObjects.User? CurrentUser = null)
    {
        List<DataObjects.ManualCheckResult> output = new List<DataObjects.ManualCheckResult>();

        List<Guid> savedIds = new List<Guid>();
        Guid siteId = Guid.Empty;

        try {
            DateTime now = DateTime.UtcNow;

            foreach (DataObjects.ManualCheckResult item in Items) {
                bool newRecord = false;
                EFModels.EFModels.ManualCheckResult? rec = null;

                if (item.ManualCheckResultId == Guid.Empty) {
                    newRecord = true;
                    item.ManualCheckResultId = Guid.NewGuid();

                    rec = new EFModels.EFModels.ManualCheckResult {
                        ManualCheckResultId = item.ManualCheckResultId,
                        SiteId = item.SiteId,
                    };
                } else {
                    rec = await data.ManualCheckResults.FirstOrDefaultAsync(x => x.ManualCheckResultId == item.ManualCheckResultId);

                    if (rec == null) {
                        continue;
                    }
                }

                siteId = item.SiteId;

                rec.WcagCriterion = item.WcagCriterion;
                rec.Status = item.Status;
                rec.Notes = item.Notes;

                // Auto-set TestedBy if empty and CurrentUser is available
                if (String.IsNullOrWhiteSpace(item.TestedBy) && CurrentUser != null && !String.IsNullOrWhiteSpace(CurrentUser.DisplayName)) {
                    rec.TestedBy = CurrentUser.DisplayName;
                } else {
                    rec.TestedBy = item.TestedBy;
                }

                // Auto-set TestedAt if status is not "Not Tested" and TestedAt is null
                if (item.TestedAt == null && !String.IsNullOrWhiteSpace(item.Status) && item.Status != "Not Tested") {
                    rec.TestedAt = now;
                } else {
                    rec.TestedAt = item.TestedAt;
                }

                if (newRecord) {
                    await data.ManualCheckResults.AddAsync(rec);
                }

                await data.SaveChangesAsync();
                savedIds.Add(item.ManualCheckResultId);
            }

            if (savedIds.Any()) {
                output = await GetManualChecks(savedIds, siteId);
            }
        } catch (Exception ex) {
            DataObjects.ManualCheckResult errorItem = new DataObjects.ManualCheckResult();
            errorItem.ActionResponse.Messages.Add("Error Saving Manual Checks");
            errorItem.ActionResponse.Messages.AddRange(RecurseException(ex));
            output.Add(errorItem);
        }

        return output;
    }
}
