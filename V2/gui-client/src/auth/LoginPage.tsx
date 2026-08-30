import { useState, type FormEvent } from "react";
import { Navigate, useLocation } from "react-router";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Paper from "@mui/material/Paper";
import TextField from "@mui/material/TextField";
import { useAuth } from "./AuthContext";
import { Logo } from "../theme/Logo";

export function LoginPage() {
  const { person, isLoading, login } = useAuth();
  const location = useLocation();
  const [externalLogin, setExternalLogin] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  if (!isLoading && person) {
    const from = (location.state as { from?: string } | null)?.from ?? "/";
    return <Navigate to={from} replace />;
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await login(externalLogin, password);
    } catch {
      setError("Login failed — check the login name and password.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <Box
      sx={{
        display: "flex",
        minHeight: "100vh",
        alignItems: "center",
        justifyContent: "center",
        bgcolor: "grey.100",
      }}
    >
      <Paper component="form" onSubmit={handleSubmit} elevation={3} sx={{ p: 4, width: 360 }}>
        <Box sx={{ mb: 2 }}>
          <Logo variant="h5" />
        </Box>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}
        <TextField
          label="Login name"
          fullWidth
          margin="normal"
          autoFocus
          value={externalLogin}
          onChange={(event) => setExternalLogin(event.target.value)}
        />
        <TextField
          label="Password"
          type="password"
          fullWidth
          margin="normal"
          value={password}
          onChange={(event) => setPassword(event.target.value)}
        />
        <Button
          type="submit"
          variant="contained"
          fullWidth
          sx={{ mt: 2 }}
          disabled={isSubmitting || !externalLogin || !password}
        >
          Log in
        </Button>
      </Paper>
    </Box>
  );
}
