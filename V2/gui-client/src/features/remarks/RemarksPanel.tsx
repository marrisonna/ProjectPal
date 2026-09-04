import { useState } from "react";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import CircularProgress from "@mui/material/CircularProgress";
import Paper from "@mui/material/Paper";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useCreateRemark, useRemarks, usePeople, type RemarkOwner } from "../../api/hooks";

// Modernised per Plan.md §5: no separate RemarkWindow — an inline comment
// thread, built once here and reused by Task/Project/Component Detail.
export function RemarksPanel({
  owner,
  hideHeading = false,
}: {
  owner: RemarkOwner;
  hideHeading?: boolean;
}) {
  const { data: remarks, isLoading } = useRemarks(owner);
  const { data: people } = usePeople();
  const createRemark = useCreateRemark(owner);
  const [draft, setDraft] = useState("");

  function personName(personId: number) {
    return people?.find((p) => p.person_id === personId)?.name ?? `Person #${personId}`;
  }

  async function handleAdd() {
    if (!draft.trim()) return;
    await createRemark.mutateAsync(draft.trim());
    setDraft("");
  }

  return (
    <Box>
      {!hideHeading && (
        <Typography variant="subtitle1" gutterBottom>
          Remarks
        </Typography>
      )}
      {isLoading && <CircularProgress size={20} />}
      <Stack spacing={1} sx={{ mb: 2 }}>
        {remarks?.map((remark) => (
          <Paper key={remark.remark_id} variant="outlined" sx={{ p: 1.5 }}>
            <Typography variant="body2">{remark.remark_text}</Typography>
            <Typography variant="caption" color="text.secondary">
              {personName(remark.created_by_person_id)} —{" "}
              {new Date(remark.created_time).toLocaleString()}
            </Typography>
          </Paper>
        ))}
        {remarks?.length === 0 && (
          <Typography variant="body2" color="text.secondary">
            No remarks yet.
          </Typography>
        )}
      </Stack>
      <Stack direction="row" spacing={1}>
        <TextField
          size="small"
          fullWidth
          placeholder="Add a remark…"
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
        />
        <Button variant="outlined" onClick={handleAdd} disabled={!draft.trim()}>
          Add
        </Button>
      </Stack>
    </Box>
  );
}
