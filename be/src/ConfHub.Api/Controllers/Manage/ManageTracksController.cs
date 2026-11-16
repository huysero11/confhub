using ConfHub.Api.Authorization;
using ConfHub.Api.Controllers.Manage.Requests;
using ConfHub.Application.Common;
using ConfHub.Application.Tracks;
using ConfHub.Application.Tracks.CreateTrack;
using ConfHub.Application.Tracks.DeleteTrack;
using ConfHub.Application.Tracks.GetTracks;
using ConfHub.Application.Tracks.UpdateTrack;
using ConfHub.Domain.Accounts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ConfHub.Api.Controllers.Manage;

[ApiController]
[Route("api/manage/conferences/{conferenceId:guid}/tracks")]
[MustHavePermission(Permissions.SessionManage)]
public sealed class ManageTracksController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<TrackResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTracks(
        Guid conferenceId,
        CancellationToken cancellationToken)
    {
        var query = new GetTracksQuery(conferenceId);
        var response = await sender.Send(query, cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType<CreatedIdResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTrack(
        Guid conferenceId,
        TrackRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateTrackCommand(conferenceId, request.NameVi, request.NameEn);
        var response = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetTracks), new { conferenceId }, response);
    }

    [HttpPut("{trackId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateTrack(
        Guid conferenceId,
        Guid trackId,
        TrackRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTrackCommand(conferenceId, trackId, request.NameVi, request.NameEn);
        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{trackId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteTrack(
        Guid conferenceId,
        Guid trackId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteTrackCommand(conferenceId, trackId);
        await sender.Send(command, cancellationToken);
        return NoContent();
    }
}
