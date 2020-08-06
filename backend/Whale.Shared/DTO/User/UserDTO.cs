﻿using System;

namespace Whale.Shared.DTO.User
{
    public class UserDTO
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string SecondName { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string AvatarUrl { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }
}
