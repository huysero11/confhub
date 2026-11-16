using ConfHub.Api.Authorization;
using ConfHub.Api.Controllers.Manage.Requests;
using ConfHub.Application.Common;
using ConfHub.Application.Conferences.AddBreakSlot;
using ConfHub.Application.Conferences.AddConferenceDay;
using ConfHub.Application.Conferences.DeleteBreakSlot;
using ConfHub.Application.Conferences.DeleteConferenceDay;
using ConfHub.Application.Conferences.UpdateBreakSlot;
using ConfHub.Application.Conferences.UpdateConferenceDay;
using ConfHub.Domain.Accounts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ConfHub.Api.Controllers.Manage;

[ApiController]
[Route("api/manage/conferences/{conferenceId:guid}/days")]
[MustHavePermission(Permissions.ConferenceManage)]
public sealed class ManageConferenceDaysController(ISender sender) : ControllerBase
{
    // ---------- NGÀY TỔ CHỨC ----------
    [HttpPost]
    [ProducesResponseType<CreatedIdResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> AddConferenceDay(
        Guid conferenceId,
        ConferenceDayRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddConferenceDayCommand(conferenceId, request.Date, request.StartTime, request.EndTime);
        var response = await sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{dayId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateConferenceDay(
        Guid conferenceId,
        Guid dayId,
        ConferenceDayRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateConferenceDayCommand(conferenceId, dayId, request.Date, request.StartTime, request.EndTime);
        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{dayId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteConferenceDay(
        Guid conferenceId,
        Guid dayId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteConferenceDayCommand(conferenceId, dayId);
        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    // ---------- KHUNG NGHỈ ----------
    [HttpPost("{dayId:guid}/breaks")]
    [ProducesResponseType<CreatedIdResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> AddBreakSlot(
        Guid conferenceId,
        Guid dayId,
        BreakSlotRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddBreakSlotCommand(conferenceId, dayId, request.Name, request.StartTime, request.EndTime);
        var response = await sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{dayId:guid}/breaks/{breakId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateBreakSlot(
        Guid conferenceId,
        Guid dayId,
        Guid breakId,
        BreakSlotRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBreakSlotCommand(conferenceId, dayId, breakId, request.Name, request.StartTime, request.EndTime);
        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{dayId:guid}/breaks/{breakId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteBreakSlot(
        Guid conferenceId,
        Guid dayId,
        Guid breakId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteBreakSlotCommand(conferenceId, dayId, breakId);
        await sender.Send(command, cancellationToken);
        return NoContent();
    }
}
