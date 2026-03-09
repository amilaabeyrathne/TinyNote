using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TinyNote.Api.Controllers;
using TinyNote.Api.DTOs;
using TinyNote.Api.Exceptions;
using TinyNote.Api.Services;

namespace TinyNote.Tests.Controllers;

public class NotesControllerTests
{
    private readonly Mock<INotesService> _notesServiceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly NotesController _sut;

    public NotesControllerTests()
    {
        _notesServiceMock = new Mock<INotesService>();
        _mapperMock = new Mock<IMapper>();
        _sut = new NotesController(_notesServiceMock.Object, _mapperMock.Object);
    }

    // -------------------------------------------------------------------------
    // GetNote
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetNote_NoteExists_Returns200OkWithNoteResponse()
    {
        var noteId = Guid.NewGuid();
        var noteResponse = new NoteResponse { Id = noteId, Title = "Title", Content = "Content" };

        _notesServiceMock
            .Setup(s => s.GetNoteAsync(noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(noteResponse);

        var result = await _sut.GetNote(noteId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(noteResponse);
    }

    [Fact]
    public async Task GetNote_NoteDoesNotExist_Returns404NotFound()
    {
        var noteId = Guid.NewGuid();

        _notesServiceMock
            .Setup(s => s.GetNoteAsync(noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NoteResponse?)null);

        var result = await _sut.GetNote(noteId);

        result.Should().BeOfType<NotFoundResult>()
            .Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetNote_NoteDoesNotExist_DoesNotReturnNoteBody()
    {
        var noteId = Guid.NewGuid();

        _notesServiceMock
            .Setup(s => s.GetNoteAsync(noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NoteResponse?)null);

        var result = await _sut.GetNote(noteId);

        // 404 must not carry a response body that leaks internal data
        result.Should().BeOfType<NotFoundResult>();
        result.Should().NotBeOfType<NotFoundObjectResult>();
    }

    // -------------------------------------------------------------------------
    // DeleteNote
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteNote_NoteExists_Returns204NoContent()
    {
        var noteId = Guid.NewGuid();

        _notesServiceMock
            .Setup(s => s.DeleteNoteAsync(noteId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.DeleteNote(noteId);

        result.Should().BeOfType<NoContentResult>()
            .Which.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task DeleteNote_ServiceThrowsItemNotFoundException_ExceptionPropagates()
    {
        var noteId = Guid.NewGuid();

        _notesServiceMock
            .Setup(s => s.DeleteNoteAsync(noteId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ItemNotFoundException(noteId));

        var act = async () => await _sut.DeleteNote(noteId);

        await act.Should().ThrowAsync<ItemNotFoundException>();
    }
}
