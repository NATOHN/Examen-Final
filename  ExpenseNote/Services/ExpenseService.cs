using ExpenseNote.DTOs;
using Google.Cloud.Firestore;

namespace ExpenseNote.Services;

public class ExpenseService
{
    private readonly FirestoreDb _db;
    private const string CollectionName = "expenses";

    public ExpenseService(FirestoreDb db)
    {
        _db = db;
    }

    public async Task<ExpenseResponseDto> CreateExpenseAsync(CreateExpenseDto dto, string userId)
    {
        var expenseId = Guid.NewGuid().ToString();
        var createdAt = DateTime.UtcNow;

        var data = new Dictionary<string, object>
        {
            { "Description", dto.Description },
            { "Amount", (double)dto.Amount },
            { "Category", dto.Category },
            { "UserId", userId },
            { "CreatedAt", Timestamp.FromDateTime(createdAt) }
        };

        await _db.Collection(CollectionName).Document(expenseId).SetAsync(data);

        return new ExpenseResponseDto
        {
            Id = expenseId,
            Description = dto.Description,
            Amount = dto.Amount,
            Category = dto.Category,
            CreatedAt = createdAt
        };
    }

    public async Task<List<ExpenseResponseDto>> GetExpensesByUserIdAsync(string userId)
    {
        var snapshot = await _db.Collection(CollectionName)
            .WhereEqualTo("UserId", userId)
            .GetSnapshotAsync();

        var result = new List<ExpenseResponseDto>();

        foreach (var doc in snapshot.Documents)
        {
            var amountRaw = doc.GetValue<object>("Amount");
            decimal amount = amountRaw switch
            {
                long l => Convert.ToInt32(l),
                double d => (decimal)d,
                _ => 0
            };

            var createdAtTimestamp = doc.GetValue<Timestamp>("CreatedAt");

            result.Add(new ExpenseResponseDto
            {
                Id = doc.Id,
                Description = doc.GetValue<string>("Description"),
                Amount = amount,
                Category = doc.GetValue<string>("Category"),
                CreatedAt = createdAtTimestamp.ToDateTime()
            });
        }

        return result.OrderByDescending(e => e.CreatedAt).ToList();
    }
}