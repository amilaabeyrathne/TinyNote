using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using TinyNote.Api.DTOs;
using TinyNote.Api.Services;

namespace TinyNote.Api.Controllers
{
    [ApiController]
    [Route("api/notes")]
    public class NotesController : ControllerBase
    {
        private readonly INotesService _notesService;
        private readonly IMapper _mapper;

        public NotesController(INotesService notesService, IMapper mapper)
        {
            _notesService = notesService;
            _mapper = mapper;
        }

        [HttpPost]
        [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddNote([FromBody] CreateNoteRequest request, CancellationToken cancellationToken = default)
        {
            var response = await _notesService.AddNoteAsync(request, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetNote(Guid id, CancellationToken cancellationToken = default)
        {
            var note = await _notesService.GetNoteAsync(id, cancellationToken);
            if (note is null)
                return NotFound();
            return Ok(_mapper.Map<NoteResponse>(note));
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<NoteResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNotes([FromQuery] Guid userId, CancellationToken cancellationToken = default)
        {
            var notes = await _notesService.GetNotesAsync(userId, cancellationToken);
            return Ok(_mapper.Map<List<NoteResponse>>(notes));
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteNote(Guid id, CancellationToken cancellationToken = default)
        {
            var isDeleted = await _notesService.DeleteNoteAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpPut]
        [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateNote([FromBody] UpdateNoteRequest request, CancellationToken cancellationToken = default)
        {
            var response = await _notesService.UpdateNoteAsync(request, cancellationToken);
            return Ok(response);
        }
    }
}
