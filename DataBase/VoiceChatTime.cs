//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан по шаблону.
//
//     Изменения, вносимые в этот файл вручную, могут привести к непредвиденной работе приложения.
//     Изменения, вносимые в этот файл вручную, будут перезаписаны при повторном создании кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace supportBotGaga
{
    using System;
    using System.Collections.Generic;
    
    public partial class VoiceChatTime
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public System.DateTime StartTime { get; set; }
        public System.DateTime EndTime { get; set; }
        public System.DateTime TotalTime { get; set; }
    
        public virtual User User { get; set; }
    }
}