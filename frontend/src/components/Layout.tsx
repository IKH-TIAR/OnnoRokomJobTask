import { NavLink, Outlet } from 'react-router'
import { StudentIdBar } from './StudentIdBar'

const navClass = ({ isActive }: { isActive: boolean }) =>
  `rounded-md px-3 py-1.5 text-sm font-medium ${
    isActive ? 'bg-indigo-600 text-white' : 'text-slate-700 hover:bg-slate-100'
  }`

export function Layout() {
  return (
    <div className="min-h-screen bg-slate-50 text-slate-900">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-5xl flex-col gap-3 px-4 py-4">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <h1 className="text-lg font-bold">🏫 School Admission</h1>
            <nav className="flex flex-wrap gap-1">
              <NavLink to="/" end className={navClass}>
                Classes
              </NavLink>
              <NavLink to="/my-applications" className={navClass}>
                My applications
              </NavLink>
              <NavLink to="/admin" className={navClass}>
                Admin
              </NavLink>
            </nav>
          </div>
          <StudentIdBar />
        </div>
      </header>
      <main className="mx-auto max-w-5xl px-4 py-6">
        <Outlet />
      </main>
    </div>
  )
}
