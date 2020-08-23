﻿using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Whale.Shared.Models.Contact;
using Whale.Shared.Services;

namespace Whale.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ContactsController : ControllerBase
    {
        private readonly ContactsService _contactsService;

        public ContactsController(ContactsService contactsService)
        {
            _contactsService = contactsService;
            
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            string email = HttpContext?.User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            Console.WriteLine("email");
            Console.WriteLine(email);
            var contacts = await _contactsService.GetAllContactsAsync(email);
            if (contacts == null) return NotFound();

            return Ok(contacts);
        }

        [HttpGet("id/{contactId}")]
        public async Task<IActionResult> Get(Guid contactId)
        {
            string email = HttpContext?.User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            var contact = await _contactsService.GetContactAsync(contactId, email);

            if (contact == null) return NotFound();

            return Ok(contact);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateFromEmail([FromQuery(Name = "email")] string contactnerEmail)
        {
            var ownerEmail = HttpContext?.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var createdContact = await _contactsService.CreateContactFromEmailAsync(ownerEmail, contactnerEmail);
            return Created($"id/{createdContact.Id}", createdContact);

        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            string email = HttpContext?.User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var deleted = await _contactsService.DeleteContactAsync(id, email);

            if (deleted) return NoContent();

            return NotFound();
        }

        [HttpDelete("email/{contactEmail}")]
        public async Task<IActionResult> DeletePendingContactByEmail(string contactEmail)
        {
            string userEmail = HttpContext?.User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var deleted = await _contactsService.DeletePendingContactByEmailAsync(contactEmail, userEmail);

            if (deleted) return NoContent();

            return NotFound();
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] ContactEditDTO contactDTO)
        {
            string email = HttpContext?.User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            await _contactsService.UpdateContactAsync(contactDTO, email);

            return Ok();
        }
    }
}