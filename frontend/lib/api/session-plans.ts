import { SessionPlan, GenerateSessionPlanRequest, CreateSessionPlanRequest } from '@/types';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5059';

export async function generateSessionPlan(
  durationMinutes: number,
  focusAreas: string[]
): Promise<SessionPlan> {
  const response = await fetch(`${API_BASE_URL}/api/SessionPlans/generate`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      durationMinutes,
      focusAreas,
    } as GenerateSessionPlanRequest),
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || 'Failed to generate session plan');
  }

  return response.json();
}

export async function createSessionPlan(
  title: string,
  summary: string,
  clipIds: number[]
): Promise<SessionPlan> {
  const response = await fetch(`${API_BASE_URL}/api/SessionPlans`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      title,
      summary,
      clipIds,
    } as CreateSessionPlanRequest),
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || 'Failed to create session plan');
  }

  return response.json();
}

export async function getSessionPlans(): Promise<SessionPlan[]> {
  const response = await fetch(`${API_BASE_URL}/api/SessionPlans`);

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || 'Failed to fetch session plans');
  }

  return response.json();
}

export async function getSessionPlan(id: number): Promise<SessionPlan> {
  const response = await fetch(`${API_BASE_URL}/api/SessionPlans/${id}`);

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || 'Failed to fetch session plan');
  }

  return response.json();
}

export async function deleteSessionPlan(id: number): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/api/SessionPlans/${id}`, {
    method: 'DELETE',
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || 'Failed to delete session plan');
  }
}

