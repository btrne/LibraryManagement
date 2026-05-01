using Library.API.Entities;

namespace Library.API.Helpers
{
    public static class EnumExtensions
    {
        public static string ToFriendlyString(this CopyStatus status)
        {
            return status switch
            {
                CopyStatus.Available => "Sẵn có",
                CopyStatus.Borrowed => "Đang mượn",
                CopyStatus.Damaged => "Bị hỏng",
                CopyStatus.Lost => "Bị mất",
                _ => "Không xác định"
            };
        }
    }
}