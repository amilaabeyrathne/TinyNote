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

        result.Should().BeOfType<NotFoundResult>();
        result.Should().NotBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteNote_NoteExists_Returns204NoContent()
    {
        var noteId = Guid.NewGuid();

        _notesServiceMock
            .Setup(s => s.DeleteNoteAsync(noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.DeleteNote(noteId);

        result.Should().BeOfType<NoContentResult>()
            .Which.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task DeleteNote_NoteExists_CallsServiceExactlyOnce()
    {
        var noteId = Guid.NewGuid();

        _notesServiceMock
            .Setup(s => s.DeleteNoteAsync(noteId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await _sut.DeleteNote(noteId);

        _notesServiceMock.Verify(
            s => s.DeleteNoteAsync(noteId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteNote_NoteExists_Returns204WithNoBody()
    {
        var noteId = Guid.NewGuid();

        _notesServiceMock
            .Setup(s => s.DeleteNoteAsync(noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.DeleteNote(noteId);

        result.Should().BeOfType<NoContentResult>();
        result.Should().NotBeOfType<ObjectResult>();
    }

    [Fact]
    public async Task DeleteNote_ServiceThrowsItemNotFoundException_ExceptionPropagates()
    {
        var noteId = Guid.NewGuid();

        _notesServiceMock
            .Setup(s => s.DeleteNoteAsync(noteId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ItemNotFoundException(noteId));

        var act = async () => await _sut.DeleteNote(noteId);

        await act.Should().ThrowAsync<ItemNotFoundException>()
            .WithMessage($"*{noteId}*");
    }

    [Fact]
    public async Task DeleteNote_ServiceThrowsUnexpectedException_ExceptionPropagates()
    {
        var noteId = Guid.NewGuid();

        _notesServiceMock
            .Setup(s => s.DeleteNoteAsync(noteId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var act = async () => await _sut.DeleteNote(noteId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database error");
    }
}
