using ConfHub.Api.Authorization;
using ConfHub.Api.Controllers.Manage.Requests;
using ConfHub.Application.Common;
using ConfHub.Application.Conferences;
using ConfHub.Application.Conferences.CreateConference;
using ConfHub.Application.Conferences.DeleteConference;
using ConfHub.Application.Conferences.GetConference;
using ConfHub.Application.Conferences.GetMyConferences;
using ConfHub.Application.Conferences.UpdateConference;
using ConfHub.Domain.Accounts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ConfHub.Api.Controllers.Manage;

[ApiController]
[Route("api/manage/conferences")]
[MustHavePermission(Permissions.ConferenceManage)]
public sealed class ManageConferencesController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CreatedIdResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateConference(CreateConferenceCommand command, CancellationToken cancellationToken)
    {
        var response = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetConference), new { conferenceId = response.Id }, response);
    }

    [HttpGet]
    [ProducesResponseType<List<ConferenceListItemResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyConferences(
        CancellationToken cancellationToken)
    {
        var query = new GetMyConferencesQuery();
        var response = await sender.Send(query, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{conferenceId:guid}")]
    [ProducesResponseType<ConferenceDetailResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConference(
        Guid conferenceId,
        CancellationToken cancellationToken)
    {
        var query = new GetConferenceQuery(conferenceId);
        var response = await sender.Send(query, cancellationToken);
        return Ok(response);
    }

    [HttpPut("{conferenceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateConference(
        Guid conferenceId,
        ConferenceDetailsRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateConferenceCommand(
            conferenceId,
            request.NameVi,
            request.NameEn,
            request.DescriptionVi,
            request.DescriptionEn,
            request.Venue,
            request.RegistrationCloseAt,
            request.RequireApproval);
        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{conferenceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteConference(
        Guid conferenceId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteConferenceCommand(conferenceId);
        await sender.Send(command, cancellationToken);
        return NoContent();
    }
}
