import { useRef, useState } from "react";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import Link from "@mui/material/Link";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import ListItemIcon from "@mui/material/ListItemIcon";
import ListItemText from "@mui/material/ListItemText";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import AttachFileIcon from "@mui/icons-material/AttachFile";
import LinkIcon from "@mui/icons-material/Link";
import {
  useAttachments,
  useCreateFileAttachment,
  useCreateLinkAttachment,
  type RemarkOwner,
} from "../../api/hooks";
import { apiClient } from "../../api/client";

export function AttachmentsPanel({
  owner,
  hideHeading = false,
}: {
  owner: RemarkOwner;
  hideHeading?: boolean;
}) {
  const { data: attachments } = useAttachments(owner);
  const createLink = useCreateLinkAttachment(owner);
  const createFile = useCreateFileAttachment(owner);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [linkDialogOpen, setLinkDialogOpen] = useState(false);
  const [linkName, setLinkName] = useState("");
  const [linkUrl, setLinkUrl] = useState("");

  async function handleAddLink() {
    if (!linkName.trim() || !linkUrl.trim()) return;
    await createLink.mutateAsync({ name: linkName.trim(), url: linkUrl.trim() });
    setLinkDialogOpen(false);
    setLinkName("");
    setLinkUrl("");
  }

  async function handleFilePicked(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    if (!file) return;
    await createFile.mutateAsync({ name: file.name, file });
    event.target.value = "";
  }

  async function handleDownload(attachmentId: number, name: string) {
    const { data } = await apiClient.GET("/attachment/{attachment_id}/download", {
      params: { path: { attachment_id: attachmentId } },
      parseAs: "blob",
    });
    if (!data) return;
    const url = URL.createObjectURL(data as Blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = name;
    link.click();
    URL.revokeObjectURL(url);
  }

  return (
    <Box>
      <Box sx={{ display: "flex", justifyContent: hideHeading ? "flex-end" : "space-between", alignItems: "center" }}>
        {!hideHeading && <Typography variant="subtitle1">Attachments</Typography>}
        <Box>
          <Button size="small" onClick={() => setLinkDialogOpen(true)}>
            Add Link
          </Button>
          <Button size="small" onClick={() => fileInputRef.current?.click()}>
            Add File
          </Button>
          <input ref={fileInputRef} type="file" hidden onChange={handleFilePicked} />
        </Box>
      </Box>
      <List dense>
        {attachments?.map((attachment) => (
          <ListItem key={attachment.attachment_id}>
            <ListItemIcon sx={{ minWidth: 32 }}>
              {attachment.kind === "Link" ? (
                <LinkIcon fontSize="small" />
              ) : (
                <AttachFileIcon fontSize="small" />
              )}
            </ListItemIcon>
            {attachment.kind === "Link" ? (
              <ListItemText
                primary={
                  <Link href={attachment.url ?? "#"} target="_blank" rel="noopener">
                    {attachment.name}
                  </Link>
                }
              />
            ) : (
              <ListItemText
                primary={
                  <Link
                    component="button"
                    onClick={() => handleDownload(attachment.attachment_id, attachment.name)}
                  >
                    {attachment.name}
                  </Link>
                }
              />
            )}
          </ListItem>
        ))}
        {attachments?.length === 0 && (
          <Typography variant="body2" color="text.secondary" sx={{ pl: 2 }}>
            No attachments yet.
          </Typography>
        )}
      </List>

      <Dialog open={linkDialogOpen} onClose={() => setLinkDialogOpen(false)} fullWidth maxWidth="xs">
        <DialogTitle>Add Link</DialogTitle>
        <DialogContent>
          <TextField
            label="Name"
            fullWidth
            margin="normal"
            value={linkName}
            onChange={(event) => setLinkName(event.target.value)}
          />
          <TextField
            label="URL"
            fullWidth
            margin="normal"
            value={linkUrl}
            onChange={(event) => setLinkUrl(event.target.value)}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setLinkDialogOpen(false)}>Cancel</Button>
          <Button variant="contained" onClick={handleAddLink}>
            Add
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
