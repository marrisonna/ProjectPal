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

// GET /auth/whoami and GET /team have no response_model on the API side
// (rest-api/app/routes/{auth,teams}.py), so openapi-typescript can't infer
// their shape — typed here from each handler's actual return.
export interface WhoAmI {
  person_id: number;
  is_organisation_admin: boolean;
  team_roles: { team_id: number; role: string; is_resource: boolean }[];
}

export interface Team {
  team_id: number;
  name: string;
}
