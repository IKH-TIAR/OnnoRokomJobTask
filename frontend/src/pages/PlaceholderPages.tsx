import { Link, useParams } from 'react-router'

// Temporary screens; replaced in the next step.

export function ApplicationPage() {
  const { holdId } = useParams()
  return (
    <section className="flex flex-col gap-2">
      <h2 className="text-xl font-bold">Application submitted</h2>
      <p className="text-slate-600">
        Application <span className="font-mono">{holdId}</span> was created. The detail screen (countdown, pay fee, cancel) comes next.
      </p>
      <Link to="/" className="text-indigo-600 hover:underline">
        Back to classes
      </Link>
    </section>
  )
}

export function MyApplicationsPage() {
  return <p className="text-slate-600">My applications: coming in the next step.</p>
}
