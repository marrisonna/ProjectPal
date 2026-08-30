import { useQuery } from "@tanstack/react-query";
import Alert from "@mui/material/Alert";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import CircularProgress from "@mui/material/CircularProgress";
import Typography from "@mui/material/Typography";
import { apiClient, type Team } from "../api/client";

export function Dashboard() {
  const { data, error, isLoading } = useQuery({
    queryKey: ["teams"],
    queryFn: async () => {
      const { data, error } = await apiClient.GET("/team");
      if (error) throw error;
      return data as unknown as Team[];
    },
  });

  return (
    <div>
      <Typography variant="h5" component="h1" gutterBottom>
        Dashboard
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Placeholder for the per-Resource workload report
        (Claude/Level1_Implementation/4_GuiClient/Plan.md §6.1) — real content lands once
        Task data exists to summarise, from Stage 2 onward.
      </Typography>
      {isLoading && <CircularProgress size={24} />}
      {error != null && (
        <Alert severity="error">Could not reach the API — is it running?</Alert>
      )}
      {data && (
        <Card sx={{ maxWidth: 320 }}>
          <CardContent>
            <Typography variant="overline" color="text.secondary">
              Teams
            </Typography>
            <Typography variant="h4">{data.length}</Typography>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
