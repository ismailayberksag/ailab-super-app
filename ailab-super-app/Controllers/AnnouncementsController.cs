using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ailab_super_app.DTOs.Announcement;
using ailab_super_app.Helpers;
using ailab_super_app.Services.Interfaces;

namespace ailab_super_app.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AnnouncementsController : ControllerBase
    {
        private readonly IAnnouncementService _announcementService;

        public AnnouncementsController(IAnnouncementService announcementService)
        {
            _announcementService = announcementService;
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> Create([FromBody] CreateAnnouncementDto dto)
        {
            var actorUserId = GetCurrentUserId();
            var id = await _announcementService.CreateAsync(actorUserId, dto);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }

        [HttpGet("my")]
        public async Task<ActionResult<PagedResult<AnnouncementListDto>>> GetMine([FromQuery] PaginationParams pagination, [FromQuery] bool? isRead = null)
        {
            var userId = GetCurrentUserId();
            var result = await _announcementService.GetMyAnnouncementsAsync(userId, pagination, isRead);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<AnnouncementDto>> GetById(Guid id)
        {
            var requesterId = GetCurrentUserId();
            var dto = await _announcementService.GetByIdAsync(id, requesterId);
            return Ok(dto);
        }

        [HttpPut("{id:guid}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var userId = GetCurrentUserId();
            await _announcementService.MarkAsReadAsync(id, userId);
            return NoContent();
        }

        private Guid GetCurrentUserId()
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return Guid.Parse(sub!);
        }
    }
}