using System.Collections.Generic;

namespace SinetLeaveManagement.Models
{
    public class EditRolesViewModel
    {
        public string UserId { get; set; }
        public string UserEmail { get; set; }
        public List<string> AllRoles { get; set; }
        public IList<string> AssignedRoles { get; set; }
        public List<string> SelectedRoles { get; set; }
    }
}
