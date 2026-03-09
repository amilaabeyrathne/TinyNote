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

        public NotesController(INotesService notesService)
        {
            _notesService = notesService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status201Created)]
        public async Task<IActionResult> AddNote([FromBody] CreateNoteRequest request, CancellationToken cancellationToken = default)
        {
            var response = await _notesService.AddNoteAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetNote), new { id = response.Id }, response);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetNote(Guid id, CancellationToken cancellationToken = default)
        {
            var note = await _notesService.GetNoteAsync(id, cancellationToken);
            if (note is null)
                return NotFound();
            return Ok(note);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<NoteResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetNotes([FromQuery] GetNotesQuery query, CancellationToken cancellationToken = default)
        {
            var notes = await _notesService.GetNotesAsync(query, cancellationToken);
            return Ok(notes);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteNote(Guid id, CancellationToken cancellationToken = default)
        {
            await _notesService.DeleteNoteAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateNote(Guid id, [FromBody] UpdateNoteRequest request, CancellationToken cancellationToken = default)
        {
            var response = await _notesService.UpdateNoteAsync(id, request, cancellationToken);
            return Ok(response);
        }
    }
}
