import createClient from "openapi-fetch";
import type { paths } from "./schema";

export const apiClient = createClient<paths>({
  baseUrl: import.meta.env.VITE_API_BASE_URL,
});

let authToken: string | null = null;

export function setAuthToken(token: string | null) {
  authToken = token;
}

apiClient.use({
  onRequest({ request }) {
    if (authToken) {
      request.headers.set("Authorization", `Bearer ${authToken}`);
    }
    return request;
  },
});

// GET /auth/whoami has no response_model on the API side
// (rest-api/app/routes/auth.py), so openapi-typescript can't infer its
// shape — typed here from the handler's actual return. (GET /team is the
// same situation — see api/types.ts's own TeamRecord.)
export interface WhoAmI {
  person_id: number;
  is_organisation_admin: boolean;
  team_roles: { team_id: number; role: string; is_resource: boolean }[];
}
