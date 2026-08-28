/*  hold exact string values allowed for MySQL DB
    ENUM('Scheduled', 'Live', 'Ended', 'Cancelled')
*/

namespace StreamService.Models;     // file-scoped namespace declaration. keep code organized without unnecessary indentation


//declare static utility class 
public static class StreamStatus
{
    public const string Scheduled = "Scheduled";
    public const string Live = "Live";
    public const string Ended = "Ended";
    public const string Cancelled = "Cancelled";
}
